using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Collections;
public class ThaiSplit 
{

    public static IEnumerator Init()
    {
        bool done = false;
        GameStore.WebGLService.Streaming.OnLoadText("dictionary.txt", text => { 
            done = true;
            dictionary = new HashSet<string>();
            foreach (var line in text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string word = line.Trim();
                if (!string.IsNullOrEmpty(word))
                    dictionary.Add(word);
            }
        });
        yield return new WaitWhile(()=> !done);
    }


    private static HashSet<string> dictionary;
    public static string Split(string text)
    {
        if (dictionary == null)
            return text;

        var sb = new StringBuilder();
        int i = 0;
        while (i < text.Length)
        {
            string matched = null;
            int maxLen = 0;

            for (int j = 1; j <= text.Length - i; j++)
            {
                string word = text.Substring(i, j);
                if (dictionary.Contains(word) && j > maxLen)
                {
                    matched = word;
                    maxLen = j;
                }
            }

            if (matched != null)
            {
                //Debug.LogWarning($"@Match: '{matched}' at index {i}");
                sb.Append(matched).Append('\u200B');
                i += maxLen;
            }
            else
            {
                //Debug.LogWarning($"@No match for: '{text[i]}' at index {i}");
                sb.Append(text[i]);
                i++;
            }
        }

        return sb.ToString();
    }








}
