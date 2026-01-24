using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Journal_app.Models;

namespace Journal_app.Services
{
    public class JournalService
    {
        private List<JournalEntry> _entries = new();
        private int _nextId = 1;
        private readonly string _filePath;

        public JournalService()
        {
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "journal_data.json");
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    _entries = JsonSerializer.Deserialize<List<JournalEntry>>(json) ?? new();
                    _nextId = _entries.Any() ? _entries.Max(e => e.Id) + 1 : 1;
                }
            }
            catch { }
        }

        private void SaveData()
        {
            try
            {
                var json = JsonSerializer.Serialize(_entries);
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }

        public List<JournalEntry> GetAllEntries()
        {
            return _entries.OrderByDescending(e => e.EntryDate).ToList();
        }

        public JournalEntry? GetEntryByDate(DateTime date)
        {
            return _entries.FirstOrDefault(e => e.EntryDate.Date == date.Date);
        }

        public JournalEntry? GetEntryById(int id)
        {
            return _entries.FirstOrDefault(e => e.Id == id);
        }

        public void AddEntry(JournalEntry entry)
        {
            entry.Id = _nextId++;
            entry.WordCount = CountWords(entry.Content);
            entry.CreatedAt = DateTime.Now;
            entry.UpdatedAt = DateTime.Now;
            _entries.Add(entry);
            SaveData();
        }

        public void UpdateEntry(JournalEntry entry)
        {
            var existing = _entries.FirstOrDefault(e => e.Id == entry.Id);
            if (existing != null)
            {
                existing.Title = entry.Title;
                existing.Content = entry.Content;
                existing.PrimaryMood = entry.PrimaryMood;
                existing.SecondaryMood1 = entry.SecondaryMood1;
                existing.SecondaryMood2 = entry.SecondaryMood2;
                existing.Category = entry.Category;
                existing.Tags = entry.Tags;
                existing.WordCount = CountWords(entry.Content);
                existing.UpdatedAt = DateTime.Now;
                SaveData();
            }
        }

        public void DeleteEntry(int id)
        {
            var entry = _entries.FirstOrDefault(e => e.Id == id);
            if (entry != null)
            {
                _entries.Remove(entry);
                SaveData();
            }
        }

        public List<JournalEntry> SearchEntries(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return GetAllEntries();

            return _entries.Where(e =>
                (e.Title?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Content?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Tags?.Any(t => t.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ?? false)
            ).OrderByDescending(e => e.EntryDate).ToList();
        }

        public Dictionary<string, int> GetMoodDistribution()
        {
            var distribution = new Dictionary<string, int>
            {
                { "Positive", 0 },
                { "Neutral", 0 },
                { "Negative", 0 }
            };

            foreach (var entry in _entries)
            {
                var category = MoodConstants.GetMoodCategory(entry.PrimaryMood);
                if (distribution.ContainsKey(category))
                    distribution[category]++;
            }

            return distribution;
        }

        public (string Mood, int Count) GetMostFrequentMood()
        {
            if (!_entries.Any())
                return ("None", 0);

            var moodCount = _entries
                .GroupBy(e => e.PrimaryMood)
                .Select(g => (Mood: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            return moodCount;
        }

        public (int Current, int Longest, int Missed) CalculateStreaks()
        {
            if (!_entries.Any())
                return (0, 0, 0);

            var dates = _entries.Select(e => e.EntryDate.Date).Distinct().OrderByDescending(d => d).ToList();
            var today = DateTime.Today;

            bool hasTodayOrYesterday = false;
            foreach (var date in dates)
            {
                if (date == today || date == today.AddDays(-1))
                {
                    hasTodayOrYesterday = true;
                    break;
                }
            }

            int currentStreak = hasTodayOrYesterday ? 1 : 0;
            int longestStreak = 1;
            int tempStreak = 1;
            int missedDays = 0;

            for (int i = 0; i < dates.Count - 1; i++)
            {
                var daysDiff = (dates[i] - dates[i + 1]).Days;

                if (daysDiff == 1)
                {
                    tempStreak++;
                    if (i == 0 || currentStreak > 0)
                        currentStreak++;
                }
                else
                {
                    longestStreak = Math.Max(longestStreak, tempStreak);
                    tempStreak = 1;
                    if (i == 0) currentStreak = 0;
                    missedDays += (daysDiff - 1);
                }
            }

            longestStreak = Math.Max(longestStreak, tempStreak);
            return (currentStreak, longestStreak, Math.Min(missedDays, 30));
        }

        public List<(string Tag, int Count)> GetMostUsedTags(int limit = 10)
        {
            if (!_entries.Any())
                return new List<(string Tag, int Count)>();

            var allTags = new List<string>();
            foreach (var entry in _entries)
            {
                if (entry.Tags != null && entry.Tags.Any())
                {
                    allTags.AddRange(entry.Tags);
                }
            }

            return allTags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .GroupBy(t => t)
                .Select(g => (Tag: g.Key, Count: g.Count()))
                .OrderByDescending(x => x.Count)
                .Take(limit)
                .ToList();
        }

        public double GetAverageWordCount()
        {
            return _entries.Any() ? _entries.Average(e => e.WordCount) : 0;
        }

        private int CountWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }

        /// <summary>
        /// Get the current theme preference (light or dark)
        /// </summary>
        public string GetTheme()
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, "user_settings.json");

                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (settings != null && settings.ContainsKey("theme"))
                    {
                        return settings["theme"];
                    }
                }

                return "light"; // Default theme
            }
            catch
            {
                return "light"; // Fallback to light theme
            }
        }

        /// <summary>
        /// Save the theme preference to user settings file
        /// </summary>
        public void SaveTheme(string theme)
        {
            try
            {
                var settingsPath = Path.Combine(FileSystem.AppDataDirectory, "user_settings.json");

                Dictionary<string, string> settings;

                // Load existing settings or create new
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath);
                    settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                }
                else
                {
                    settings = new Dictionary<string, string>();
                }

                // Update theme
                settings["theme"] = theme;

                // Save to file
                var updatedJson = JsonSerializer.Serialize(settings);
                File.WriteAllText(settingsPath, updatedJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving theme: {ex.Message}");
            }
        }
    }
}