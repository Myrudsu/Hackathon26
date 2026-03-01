using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using static System.Net.Mime.MediaTypeNames;

#region Data Models

[Serializable]
public class WordList
{
    public string key;
    public List<string> values;
}

[Serializable]
public class WordDictionaryWrapper
{
    public List<WordList> entries = new List<WordList>();
}

[Serializable]
public class StringArrayWrapper
{
    public string[] words;
}

#endregion

public class Program : MonoBehaviour
{
    private string dataPath1;
    private string dataPath2;

    // Cached data (important for performance)
    private WordDictionaryWrapper cachedWrapper;
    private StringArrayWrapper cachedEnglish;

    void Awake()
    {
        InitializePaths();
        StartCoroutine(Initialize());
    }

    void InitializePaths()
    {
        dataPath1 = Path.Combine(UnityEngine.Application.persistentDataPath, "sampleprocessed.json");
        dataPath2 = Path.Combine(UnityEngine.Application.persistentDataPath, "20kprocessed.json");
    }

    IEnumerator Initialize()
    {
        if (!File.Exists(dataPath1) || !File.Exists(dataPath2))
        {
            yield return StartCoroutine(TrainerCoroutine());
        }

        LoadCachedData();
    }

    #region StreamingAssets Loading (ANDROID SAFE)

    IEnumerator ReadStreamingAsset(string fileName, Action<string> onLoaded)
    {
        string path = Path.Combine(UnityEngine.Application.streamingAssetsPath, fileName);

        UnityWebRequest request = UnityWebRequest.Get(path);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.LogError($"Failed to load {fileName}: {request.error}");
            onLoaded?.Invoke(null);
        }
        else
        {
            onLoaded?.Invoke(request.downloadHandler.text);
        }
    }

    #endregion

    #region Training

    IEnumerator TrainerCoroutine()
    {
        string sampleText = null;
        string english2k = null;

        yield return ReadStreamingAsset("sample.txt", t => sampleText = t);
        yield return ReadStreamingAsset("20k.txt", t => english2k = t);

        if (string.IsNullOrEmpty(sampleText) || string.IsNullOrEmpty(english2k))
        {
            UnityEngine.Debug.LogError("Training aborted: missing text files");
            yield break;
        }

        TrainFromText(sampleText, english2k);
    }

    void TrainFromText(string text, string english2k)
    {
        var dict = new Dictionary<string, List<(string Word, int Count)>>();

        string[] words = text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        string[] english2kwords = english2k.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < words.Length - 1; i++)
        {
            string currentWord = words[i].ToLower();
            string nextWord = words[i + 1].ToLower();

            if (!dict.ContainsKey(currentWord))
                dict[currentWord] = new List<(string, int)>();

            int index = dict[currentWord].FindIndex(x => x.Word == nextWord);

            if (index == -1)
                dict[currentWord].Add((nextWord, 1));
            else
                dict[currentWord][index] = (nextWord, dict[currentWord][index].Count + 1);
        }

        List<string> commonWords = new List<string>
        { "the", "be", "to", "of", "and", "a", "in", "that" };

        WordDictionaryWrapper wrapper = new WordDictionaryWrapper();

        foreach (var pair in dict)
        {
            var sorted = pair.Value
                .OrderByDescending(x => x.Count)
                .Where(x => x.Count > 1 && !commonWords.Contains(x.Word))
                .Select(x => x.Word)
                .ToList();

            wrapper.entries.Add(new WordList
            {
                key = pair.Key,
                values = sorted
            });
        }

        File.WriteAllText(dataPath1, JsonUtility.ToJson(wrapper, true));
        File.WriteAllText(
            dataPath2,
            JsonUtility.ToJson(new StringArrayWrapper { words = english2kwords }, true)
        );

        UnityEngine.Debug.Log("Training complete");
    }

    #endregion

    #region Cached Load

    void LoadCachedData()
    {
        cachedWrapper = JsonUtility.FromJson<WordDictionaryWrapper>(File.ReadAllText(dataPath1));
        cachedEnglish = JsonUtility.FromJson<StringArrayWrapper>(File.ReadAllText(dataPath2));
    }

    #endregion

    #region Predictor

    public string[] Predictor(string previousWord, string currentLetters)
    {
        if (cachedWrapper == null || cachedEnglish == null)
            return new string[8];

        string prev = previousWord?.ToLower() ?? "";
        string current = currentLetters?.ToLower() ?? "";

        List<string> results = new List<string>();

        if (string.IsNullOrEmpty(prev) || prev.Contains("."))
        {
            results.AddRange(cachedEnglish.words);
        }
        else
        {
            var entry = cachedWrapper.entries.FirstOrDefault(e => e.key == prev);
            if (entry != null)
                results.AddRange(entry.values);

            results.AddRange(cachedEnglish.words);
        }

        if (!string.IsNullOrEmpty(current))
            results = results.Where(w => w.StartsWith(current)).ToList();

        string[] top = results
            .Distinct()
            .Take(8)
            .ToArray();

        string[] padded = new string[8];
        for (int i = 0; i < 8; i++)
            padded[i] = i < top.Length ? top[i] : "null";

        return padded;
    }

    #endregion
}