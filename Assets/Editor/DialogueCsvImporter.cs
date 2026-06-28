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
        public DatingCharacter girlfriendA;

        public string choiceB;
        public int nextB;
        public int affectionB;
        public DatingCharacter girlfriendB;

        public string choiceC;
        public int nextC;
        public int affectionC;
        public DatingCharacter girlfriendC;

        public int moneyChange;
        public int conditionChange;
        public int gradeChange;
        public int friendshipChange;

        public string backgroundId;
        public string centerAppearanceId;
        public string leftAppearanceId;
        public string rightAppearanceId;

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
        List<Row> rows = new();

        foreach (Dictionary<string, string> raw in rawRows)
        {
            Row row = ParseRow(raw);

            if (string.IsNullOrWhiteSpace(row.dateId))
                continue;

            rows.Add(row);
        }

        Dictionary<string, List<Row>> rowsByDateId = new();

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

        Dictionary<int, List<Row>> rowsByScene = new();

        foreach (Row row in rows)
        {
            if (!rowsByScene.ContainsKey(row.sceneId))
                rowsByScene[row.sceneId] = new List<Row>();

            rowsByScene[row.sceneId].Add(row);
        }

        List<DialogueScene> scenes = new();

        foreach (KeyValuePair<int, List<Row>> pair in rowsByScene)
        {
            int sceneId = pair.Key;
            List<Row> sceneRows = pair.Value;
            sceneRows.Sort((a, b) => a.order.CompareTo(b.order));

            DialogueScene scene = new DialogueScene
            {
                sceneId = sceneId,
                nextSceneA = -1,
                nextSceneB = -1,
                nextSceneC = -1,
                girlfriendA = DatingCharacter.None,
                girlfriendB = DatingCharacter.None,
                girlfriendC = DatingCharacter.None
            };

            List<DialogueLine> lines = new();

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
                    scene.girlfriendA = row.girlfriendA;

                    scene.choiceTextB = row.choiceB;
                    scene.nextSceneB = row.nextB;
                    scene.affectionB = row.affectionB;
                    scene.girlfriendB = row.girlfriendB;

                    scene.choiceTextC = row.choiceC;
                    scene.nextSceneC = row.nextC;
                    scene.affectionC = row.affectionC;
                    scene.girlfriendC = row.girlfriendC;
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
        Row row = new();

        row.dateId = GetAny(raw, "DateID", "DateId", "DialogueID", "DialogueId");
        row.sceneId = GetIntAny(raw, 0, "SceneID", "SceneId");
        row.order = GetIntAny(raw, 0, "Order");
        row.type = GetAny(raw, "Type");

        row.speaker = GetAny(raw, "Speaker");
        row.text = GetAny(raw, "Text", "Dialogue", "Line");

        row.choiceA = GetAny(raw, "ChoiceA", "ChoiceTextA");
        row.nextA = GetIntAny(raw, -1, "NextSceneA");
        row.affectionA = GetIntAny(raw, 0, "AffectionA");
        row.girlfriendA = GetEnumAny(raw, DatingCharacter.None, "GirlfriendA");

        row.choiceB = GetAny(raw, "ChoiceB", "ChoiceTextB");
        row.nextB = GetIntAny(raw, -1, "NextSceneB");
        row.affectionB = GetIntAny(raw, 0, "AffectionB");
        row.girlfriendB = GetEnumAny(raw, DatingCharacter.None, "GirlfriendB");

        row.choiceC = GetAny(raw, "ChoiceC", "ChoiceTextC");
        row.nextC = GetIntAny(raw, -1, "NextSceneC");
        row.affectionC = GetIntAny(raw, 0, "AffectionC");
        row.girlfriendC = GetEnumAny(raw, DatingCharacter.None, "GirlfriendC");

        row.moneyChange = GetIntAny(raw, 0, "MoneyChange", "MoneyCange");
        row.conditionChange = GetIntAny(raw, 0, "ConditionChange", "ConditionCange");
        row.gradeChange = GetIntAny(raw, 0, "GradeChange", "GradesChange", "GradeCange");
        row.friendshipChange = GetIntAny(raw, 0, "FriendshipChange", "FriendshipCange", "RelationshipChange");

        row.backgroundId = GetAny(raw, "BackgroundId", "BackgroundID", "Background");
        row.centerAppearanceId = GetAny(raw, "CenterAppearanceId", "CenterAppearanceID", "CenterVisualId", "CenterVisualID");
        row.leftAppearanceId = GetAny(raw, "LeftAppearanceId", "LeftAppearanceID", "LeftVisualId", "LeftVisualID");
        row.rightAppearanceId = GetAny(raw, "RightAppearanceId", "RightAppearanceID", "RightApeearanceID", "RightVisualId", "RightVisualID");

        row.completesDate = GetBoolAny(raw, false, "CompletesDate", "CompleteDate");

        return row;
    }

    private static List<Dictionary<string, string>> ParseCsv(string csvText)
    {
        List<List<string>> table = ReadCsvTable(csvText);
        List<Dictionary<string, string>> rows = new();

        if (table.Count <= 1)
            return rows;

        List<string> headers = table[0];

        for (int i = 1; i < table.Count; i++)
        {
            List<string> values = table[i];

            if (values.Count == 0)
                continue;

            Dictionary<string, string> row = new();

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
        List<List<string>> table = new();
        List<string> row = new();

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

                if (HasContent(row))
                    table.Add(row);

                row = new List<string>();
            }
            else
            {
                cell += c;
            }
        }

        row.Add(cell);

        if (HasContent(row))
            table.Add(row);

        return table;
    }

    private static bool HasContent(List<string> row)
    {
        foreach (string cell in row)
        {
            if (!string.IsNullOrWhiteSpace(cell))
                return true;
        }

        return false;
    }

    private static string Get(Dictionary<string, string> row, string key)
    {
        return row.TryGetValue(key, out string value) ? value.Trim() : "";
    }

    private static string GetAny(Dictionary<string, string> row, params string[] keys)
    {
        foreach (string key in keys)
        {
            string value = Get(row, key);

            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return "";
    }

    private static int GetIntAny(Dictionary<string, string> row, int defaultValue, params string[] keys)
    {
        foreach (string key in keys)
        {
            string value = Get(row, key);

            if (int.TryParse(value, out int result))
                return result;
        }

        return defaultValue;
    }

    private static bool GetBoolAny(Dictionary<string, string> row, bool defaultValue, params string[] keys)
    {
        foreach (string key in keys)
        {
            string value = Get(row, key).ToLower();

            if (string.IsNullOrWhiteSpace(value))
                continue;

            return value == "true" || value == "1" || value == "yes" || value == "y";
        }

        return defaultValue;
    }

    private static T GetEnumAny<T>(Dictionary<string, string> row, T defaultValue, params string[] keys)
        where T : struct
    {
        foreach (string key in keys)
        {
            string value = Get(row, key);

            if (string.IsNullOrWhiteSpace(value))
                continue;

            if (Enum.TryParse(value, true, out T result))
                return result;

            Debug.LogWarning($"[DialogueCsvImporter] Enum parse failed. Key={key}, Value={value}");
        }

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