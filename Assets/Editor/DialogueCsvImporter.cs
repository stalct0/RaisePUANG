#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class DialogueCsvImporter
{
    private const string OutputFolder = "Assets/Dialogues/Generated";

    private class Row
    {
        public string dateId;
        public int sceneId;
        public int order;
        public string type;
        public string speaker;
        public string text;

        public string choiceA;
        public int nextA;
        public int affectionA;

        public string choiceB;
        public int nextB;
        public int affectionB;

        public int moneyChange;
        public int conditionChange;
        public int gradeChange;
        public int friendshipChange;
        
        public string backgroundId;
        public string centerAppearanceId;
        public string leftAppearanceId;
        public string rightAppearanceId;

        public NovelStoryKind storyKind;
        public DatingCharacter datingCharacter;
        public DatingLocation datingLocation;

        public bool completesDate;
    }

    [MenuItem("Tools/Dialogue/Import Selected CSV")]
    public static void ImportSelectedCsv()
    {
        TextAsset csv = Selection.activeObject as TextAsset;

        if (csv == null)
        {
            EditorUtility.DisplayDialog(
                "Dialogue Import Failed",
                "Project 창에서 CSV 파일을 선택한 뒤 실행하세요.",
                "OK"
            );
            return;
        }

        Import(csv.text);
    }

    private static void Import(string csvText)
    {
        EnsureOutputFolder();

        List<Dictionary<string, string>> rawRows = ParseCsv(csvText);
        List<Row> rows = new List<Row>();

        foreach (Dictionary<string, string> raw in rawRows)
        {
            Row row = ParseRow(raw);

            if (string.IsNullOrWhiteSpace(row.dateId))
                continue;

            rows.Add(row);
        }

        Dictionary<string, List<Row>> rowsByDateId = new Dictionary<string, List<Row>>();

        foreach (Row row in rows)
        {
            if (!rowsByDateId.ContainsKey(row.dateId))
                rowsByDateId[row.dateId] = new List<Row>();

            rowsByDateId[row.dateId].Add(row);
        }

        foreach (KeyValuePair<string, List<Row>> pair in rowsByDateId)
        {
            CreateDialogueAsset(pair.Key, pair.Value);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Dialogue Import Complete",
            $"{rowsByDateId.Count}개의 DialogueData를 생성했습니다.",
            "OK"
        );
    }

    private static void CreateDialogueAsset(string dateId, List<Row> rows)
    {
        rows.Sort((a, b) =>
        {
            int sceneCompare = a.sceneId.CompareTo(b.sceneId);
            return sceneCompare != 0 ? sceneCompare : a.order.CompareTo(b.order);
        });

        DialogueData asset = ScriptableObject.CreateInstance<DialogueData>();
        asset.dialogueId = dateId;
        asset.startSceneId = 0;

        Row first = rows[0];
        asset.storyKind = first.storyKind;
        asset.datingCharacter = first.datingCharacter;
        asset.datingLocation = first.datingLocation;

        Dictionary<int, List<Row>> rowsByScene = new Dictionary<int, List<Row>>();

        foreach (Row row in rows)
        {
            if (!rowsByScene.ContainsKey(row.sceneId))
                rowsByScene[row.sceneId] = new List<Row>();

            rowsByScene[row.sceneId].Add(row);
        }

        List<DialogueScene> scenes = new List<DialogueScene>();

        foreach (KeyValuePair<int, List<Row>> pair in rowsByScene)
        {
            int sceneId = pair.Key;
            List<Row> sceneRows = pair.Value;
            sceneRows.Sort((a, b) => a.order.CompareTo(b.order));

            DialogueScene scene = new DialogueScene();
            scene.sceneId = sceneId;
            scene.nextSceneA = -1;
            scene.nextSceneB = -1;

            List<DialogueLine> lines = new List<DialogueLine>();

            foreach (Row row in sceneRows)
            {
                if (IsType(row.type, "Dialogue"))
                {
                    lines.Add(new DialogueLine
                    {
                        speaker = row.speaker,
                        text = row.text,
                        backgroundId = row.backgroundId,
                        centerAppearanceId = row.centerAppearanceId,
                        leftAppearanceId = row.leftAppearanceId,
                        rightAppearanceId = row.rightAppearanceId
                    });

                    scene.moneyChange = row.moneyChange;
                    scene.conditionChange = row.conditionChange;
                    scene.gradeChange = row.gradeChange;
                    scene.friendshipChange = row.friendshipChange;
                    scene.completesDate = row.completesDate;
                }
                else if (IsType(row.type, "Choice"))
                {
                    scene.hasChoice = true;

                    scene.choiceTextA = row.choiceA;
                    scene.nextSceneA = row.nextA;
                    scene.affectionA = row.affectionA;

                    scene.choiceTextB = row.choiceB;
                    scene.nextSceneB = row.nextB;
                    scene.affectionB = row.affectionB;
                }
                else if (IsType(row.type, "End"))
                {
                    scene.completesDate = row.completesDate;
                }
            }

            scene.lines = lines.ToArray();
            scenes.Add(scene);
        }

        scenes.Sort((a, b) => a.sceneId.CompareTo(b.sceneId));
        asset.scenes = scenes.ToArray();

        string safeName = MakeSafeFileName(dateId);
        string path = $"{OutputFolder}/{safeName}.asset";

        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(asset, path);
    }

    private static Row ParseRow(Dictionary<string, string> raw)
    {
        Row row = new Row();

        row.dateId = Get(raw, "DateID");
        row.sceneId = GetInt(raw, "SceneID", 0);
        row.order = GetInt(raw, "Order", 0);
        row.type = Get(raw, "Type");

        row.speaker = Get(raw, "Speaker");
        row.text = Get(raw, "Text");

        row.choiceA = Get(raw, "ChoiceA");
        row.nextA = GetInt(raw, "NextSceneA", -1);
        row.affectionA = GetInt(raw, "AffectionA", 0);

        row.choiceB = Get(raw, "ChoiceB");
        row.nextB = GetInt(raw, "NextSceneB", -1);
        row.affectionB = GetInt(raw, "AffectionB", 0);

        row.moneyChange = GetInt(raw, "MoneyChange", 0);
        row.conditionChange = GetInt(raw, "ConditionChange", 0);
        row.gradeChange = GetInt(raw, "GradeChange", 0);
        row.friendshipChange = GetInt(raw, "FriendshipChange", 0);

        row.storyKind = GetEnum(raw, "StoryKind", NovelStoryKind.Normal);
        row.datingCharacter = GetEnum(raw, "DatingCharacter", DatingCharacter.None);
        row.datingLocation = GetEnum(raw, "DatingLocation", DatingLocation.None);

        row.completesDate = GetBool(raw, "CompletesDate", false);
        row.backgroundId = Get(raw, "BackgroundId");
        row.centerAppearanceId = Get(raw, "CenterAppearanceId");
        row.leftAppearanceId = Get(raw, "LeftAppearanceId");
        row.rightAppearanceId = Get(raw, "RightAppearanceId");
        return row;
    }

    private static List<Dictionary<string, string>> ParseCsv(string csvText)
    {
        List<List<string>> table = ReadCsvTable(csvText);
        List<Dictionary<string, string>> rows = new List<Dictionary<string, string>>();

        if (table.Count <= 1)
            return rows;

        List<string> headers = table[0];

        for (int i = 1; i < table.Count; i++)
        {
            List<string> values = table[i];

            if (values.Count == 0)
                continue;

            Dictionary<string, string> row = new Dictionary<string, string>();

            for (int h = 0; h < headers.Count; h++)
            {
                string key = headers[h].Trim();
                string value = h < values.Count ? values[h] : "";

                if (!string.IsNullOrWhiteSpace(key))
                    row[key] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<List<string>> ReadCsvTable(string text)
    {
        List<List<string>> table = new List<List<string>>();
        List<string> row = new List<string>();

        bool inQuotes = false;
        string cell = "";

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                row.Add(cell);
                cell = "";
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;

                row.Add(cell);
                cell = "";

                bool hasContent = false;
                for (int j = 0; j < row.Count; j++)
                {
                    if (!string.IsNullOrWhiteSpace(row[j]))
                    {
                        hasContent = true;
                        break;
                    }
                }

                if (hasContent)
                    table.Add(row);

                row = new List<string>();
            }
            else
            {
                cell += c;
            }
        }

        row.Add(cell);

        bool finalHasContent = false;
        for (int j = 0; j < row.Count; j++)
        {
            if (!string.IsNullOrWhiteSpace(row[j]))
            {
                finalHasContent = true;
                break;
            }
        }

        if (finalHasContent)
            table.Add(row);

        return table;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value.Trim() : "";
    }

    private static int GetInt(Dictionary<string, string> row, string key, int defaultValue)
    {
        string value = Get(row, key);

        if (int.TryParse(value, out int result))
            return result;

        return defaultValue;
    }

    private static bool GetBool(Dictionary<string, string> row, string key, bool defaultValue)
    {
        string value = Get(row, key).ToLower();

        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return value == "true" || value == "1" || value == "yes" || value == "y";
    }

    private static T GetEnum<T>(Dictionary<string, string> row, string key, T defaultValue)
        where T : struct
    {
        string value = Get(row, key);

        if (Enum.TryParse(value, true, out T result))
            return result;

        return defaultValue;
    }

    private static bool IsType(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string MakeSafeFileName(string fileName)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(c.ToString(), "_");
        }

        return fileName;
    }

    private static void EnsureOutputFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Dialogues"))
            AssetDatabase.CreateFolder("Assets", "Dialogues");

        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Dialogues", "Generated");
    }
}
#endif