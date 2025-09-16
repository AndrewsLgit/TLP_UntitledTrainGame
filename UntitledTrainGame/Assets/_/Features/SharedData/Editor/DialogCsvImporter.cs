using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SharedData.Runtime;
using UnityEditor;
using UnityEngine;

namespace SharedData.Editor
{

    public class DialogCsvImporter : EditorWindow
    {
        #region Variables

        #region Private
        // --- Start of Private Variables ---
        private string _csvFolder;
        private string _outputFolder;
        private string _characterDataFolder;
        private bool _overwriteExisting = true;
        
        private const string _dialogsCsvFileName = "UTG - Dialogues.csv";
        private const string _responsesCsvFileName = "UTG - Responses.csv";
        private const string _conditionsCsvFileName = "UTG - Conditions.csv";
        // --- End of Private Variables --- 

        #endregion

        #region Public

        // --- Start of Public Variables ---
        // --- End of Public Variables --- 

        #endregion

        #endregion

        [MenuItem("Custom Tool/Import CSV Dialogs")]
        public static void Open()
        {
            GetWindow<DialogCsvImporter>("Import CSV Dialogs");
        }
        

        #region Main Methods

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CSV Source and Output", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            _csvFolder = EditorGUILayout.TextField("CSV Folder", _csvFolder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select CSV Folder", _csvFolder ?? Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _csvFolder = MakeProjectRelative(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("DialogNode Output Folder", _outputFolder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder ?? Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _outputFolder = MakeProjectRelative(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            _characterDataFolder = EditorGUILayout.TextField("CharacterData Folder", _characterDataFolder);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                var selected = EditorUtility.OpenFolderPanel("Select CharacterData Folder", _characterDataFolder ?? Application.dataPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    _characterDataFolder = MakeProjectRelative(selected);
                }
            }
            EditorGUILayout.EndHorizontal();

            _overwriteExisting = EditorGUILayout.Toggle(new GUIContent("Overwrite Existing", "When true, existing DialogNode assets with the same Id will be updated in-place; otherwise, new ones will not overwrite."), _overwriteExisting);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!IsConfigValid()))
            {
                if (GUILayout.Button("Import CSV"))
                {
                    try
                    {
                        ImportCsv();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"CSV Dialog Import FAILED: {ex.Message}\n{ex.StackTrace}");
                    }
                }
            }

            if (!IsConfigValid())
            {
                EditorGUILayout.HelpBox("Please set CSV folder, output folder, and CharacterData folder (must be inside 'Assets/').", MessageType.Info);
            }
        }
        
        
        #endregion

        #region Helpers/Utils
        
        private bool IsConfigValid()
        {
            return IsValidAssetsRelativeFolder(_csvFolder) &&
                   IsValidAssetsRelativeFolder(_outputFolder) &&
                   IsValidAssetsRelativeFolder(_characterDataFolder);
        }

        private static bool IsValidAssetsRelativeFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (!path.StartsWith("Assets")) return false;
            return AssetDatabase.IsValidFolder(path);
        }

        private static string MakeProjectRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (absolutePath.StartsWith(dataPath))
            {
                return "Assets" + absolutePath.Substring(dataPath.Length);
            }
            Debug.LogWarning("Selected folder is outside the project. Please choose a folder under 'Assets/'.");
            return null;
        }

        private void ImportCsv()
        {
            var dialogsPath = Path.Combine(_csvFolder, _dialogsCsvFileName);
            var responsesPath = Path.Combine(_csvFolder, _responsesCsvFileName);
            var conditionsPath = Path.Combine(_csvFolder, _conditionsCsvFileName);

            if (!File.Exists(dialogsPath)) throw new FileNotFoundException($"Missing {_dialogsCsvFileName} at {_csvFolder}");
            if (!File.Exists(responsesPath)) throw new FileNotFoundException($"Missing {_responsesCsvFileName} at {_csvFolder}");
            if (!File.Exists(conditionsPath)) throw new FileNotFoundException($"Missing {_conditionsCsvFileName} at {_csvFolder}");

            // Load CharacterData cache by Id
            var characterById = LoadCharacterDataById(_characterDataFolder);

            // Parse CSVs
            var dialogRows = Csv.Read(dialogsPath)
                .Select(r => DialogRow.TryParse(r, out var d) ? d : default)
                .Where(d => !string.IsNullOrEmpty(d.Id))
                .ToList();
            foreach (var row in dialogRows)
            {
                Debug.Log($"Dialog: {row.Id}:\n{row.DialogText}\nCharacter: {row.CharacterId}\nNext: {row.NextDialogId}\nFlags: {row.FlagSpec}");
            }

            var responseRows = Csv.Read(responsesPath)
                .Select(r => ResponseRow.TryParse(r, out var rr) ? rr : default)
                .Where(r => !string.IsNullOrEmpty(r.Id))
                .ToList();
            foreach (var row in responseRows)
            {
                Debug.Log($"Response: {row.Id}:\n{row.Text}\n Dialog: {row.DialogId}\nNext: {row.NextDialogId}\nFlags: {row.FlagSpec}");
            }

            var conditionRows = Csv.Read(conditionsPath)
                .Select(r => ConditionRow.TryParse(r, out var cr) ? cr : default)
                .Where(c => !string.IsNullOrEmpty(c.TargetId))
                .ToList();
            foreach (var row in conditionRows)
            {
                Debug.Log($"Condition: {row.TargetId} {row.FlagKey} {row.NeededValue} {row.Scope}");
            }

            // Build lookup for responses by dialogId and by responseId
            var responsesByDialogId = responseRows
                .GroupBy(r => r.DialogId)
                .ToDictionary(g => g.Key, g => g.ToList());
            foreach (var group in responsesByDialogId)
            {
                foreach (var rr in group.Value)
                {
                    Debug.Log($"Response for Dialog '{group.Key}': {rr.Id}:\n{rr.Text}\nNext: {rr.NextDialogId}\nFlags: {rr.FlagSpec}");
                }
            }

            // First pass: create or update DialogNode assets without linking NextNode
            var nodeById = new Dictionary<string, DialogNode>(StringComparer.OrdinalIgnoreCase);
            var createdCount = 0;
            var updatedCount = 0;

            foreach (var dr in dialogRows)
            {
                // Create or update DialogNode asset for dr.Id
                var node = FindExistingDialogNodeById(_outputFolder, dr.Id);

                if (node == null)
                {
                    node = ScriptableObject.CreateInstance<DialogNode>();
                    node.Id = dr.Id;
                    var assetName = $"SO_DialogNodeData_{SanitizeAssetName(dr.Id)}.asset";
                    var assetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(_outputFolder, assetName));
                    AssetDatabase.CreateAsset(node, assetPath);
                    createdCount++;
                }
                else
                {
                    if (!_overwriteExisting)
                    {
                        Debug.LogWarning($"Skipping existing DialogNode with Id '{dr.Id}' (Overwrite Existing is OFF).");
                        nodeById[dr.Id] = node;
                        continue;
                    }
                    updatedCount++;
                }

                node.Id = dr.Id;
                node.DialogText = dr.DialogText ?? string.Empty;
                node.FlagsToChange = ParseFlagChanges(dr.FlagSpec);

                // Character lookup
                if (!string.IsNullOrEmpty(dr.CharacterId) && characterById.TryGetValue(dr.CharacterId, out var character))
                {
                    node.Character = character;
                }
                else
                {
                    node.Character = null;
                    if (!string.IsNullOrEmpty(dr.CharacterId))
                        Debug.LogWarning($"CharacterData with Id '{dr.CharacterId}' not found for Dialog '{dr.Id}'.");
                }

                // Clear Responses and Conditions for re-assembly
                node.Responses = new List<Response>();
                node.Conditions = node.Conditions ?? new List<Condition>();
                node.Conditions.Clear();

                // NextNode will be resolved in the second pass
                node.NextNode = null;

                EditorUtility.SetDirty(node);
                nodeById[dr.Id] = node;
            }

            AssetDatabase.SaveAssets();

            // Second pass: link NextNode for dialogs and build Responses
            var responseIndexById = new Dictionary<string, (DialogNode parent, int index)>(StringComparer.OrdinalIgnoreCase);

            foreach (var dr in dialogRows)
            {
                if (!nodeById.TryGetValue(dr.Id, out var node)) continue;

                // Link dialog.NextNode if present
                if (!string.IsNullOrEmpty(dr.NextDialogId))
                {
                    if (nodeById.TryGetValue(dr.NextDialogId, out var nextNode))
                    {
                        node.NextNode = nextNode;
                    }
                    else
                    {
                        Debug.LogWarning($"NextDialogId '{dr.NextDialogId}' not found for Dialog '{dr.Id}'.");
                        node.NextNode = null;
                    }
                }
                else
                {
                    node.NextNode = null;
                }

                // Build responses for this dialog
                node.Responses = new List<Response>();
                if (responsesByDialogId.TryGetValue(dr.Id, out var respList))
                {
                    foreach (var rr in respList)
                    {
                        var response = new Response
                        {
                            Id = rr.Id,
                            Text = rr.Text ?? string.Empty,
                            NextNode = null, // set below
                            Conditions = new List<Condition>(),
                            FlagsToChange = ParseFlagChanges(rr.FlagSpec)
                        };

                        if (!string.IsNullOrEmpty(rr.NextDialogId))
                        {
                            if (nodeById.TryGetValue(rr.NextDialogId, out var nextNodeForResponse))
                            {
                                response.NextNode = nextNodeForResponse;
                            }
                            else
                            {
                                Debug.LogWarning($"Response '{rr.Id}' points to NextDialogId '{rr.NextDialogId}' which was not found.");
                                response.NextNode = null;
                            }
                        }

                        node.Responses.Add(response);
                    }

                    // Index responses by Id for the third pass (conditions)
                    for (int i = 0; i < node.Responses.Count; i++)
                    {
                        var resp = node.Responses[i];
                        if (!string.IsNullOrEmpty(resp.Id))
                            responseIndexById[resp.Id] = (node, i);
                    }
                }

                EditorUtility.SetDirty(node);
            }

            AssetDatabase.SaveAssets();

            // Third pass: apply Conditions to either Dialog or Response
            var appliedConditions = 0;
            foreach (var cr in conditionRows)
            {
                switch (cr.TargetType)
                {
                    case ConditionTargetType.Dialog:
                    {
                        if (!nodeById.TryGetValue(cr.TargetId, out var node))
                        {
                            Debug.LogWarning($"Condition target Dialog '{cr.TargetId}' not found. Skipping condition.");
                            continue;
                        }

                        node.Conditions ??= new List<Condition>();
                        node.Conditions.Add(new Condition
                        {
                            flagKey = cr.FlagKey,
                            requiredValue = cr.NeededValue,
                            scope = cr.Scope,
                        });
                        EditorUtility.SetDirty(node);
                        appliedConditions++;
                        break;
                    }
                    case ConditionTargetType.Response:
                    {
                        if (!responseIndexById.TryGetValue(cr.TargetId, out var entry))
                        {
                            Debug.LogWarning($"Condition target Response '{cr.TargetId}' not found. Skipping condition.");
                            continue;
                        }

                        var (parent, idx) = entry;
                        var resp = parent.Responses[idx];
                        resp.Conditions ??= new List<Condition>();
                        resp.Conditions.Add(new Condition
                        {
                            flagKey = cr.FlagKey,
                            requiredValue = cr.NeededValue,
                            scope = cr.Scope,
                        });
                        parent.Responses[idx] = resp;
                        EditorUtility.SetDirty(parent);
                        appliedConditions++;
                        break;
                    }
                    default:
                        Debug.LogWarning($"Unknown Condition TargetType '{cr.TargetTypeRaw}' for target '{cr.TargetId}'.");
                        break;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"CSV Dialog Import DONE. Created: {createdCount}, Updated: {updatedCount}, Conditions applied: {appliedConditions}");
        }

        private static Dictionary<string, CharacterData> LoadCharacterDataById(string folder)
        {
            var dict = new Dictionary<string, CharacterData>(StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:CharacterData", new[] { folder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
                if (asset == null) continue;
                if (string.IsNullOrEmpty(asset.Id))
                {
                    Debug.LogWarning($"CharacterData at '{path}' has empty Id; skipping.");
                    continue;
                }
                if (!dict.ContainsKey(asset.Id))
                    dict.Add(asset.Id, asset);
                else
                    Debug.LogWarning($"Duplicate CharacterData Id '{asset.Id}' found at '{path}'. First one will be used.");
            }
            return dict;
        }

        private static DialogNode FindExistingDialogNodeById(string searchFolder, string id)
        {
            var guids = AssetDatabase.FindAssets("t:DialogNode", new[] { searchFolder });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<DialogNode>(path);
                if (asset == null) continue;
                if (string.Equals(asset.Id, id, StringComparison.OrdinalIgnoreCase))
                    return asset;
            }
            return null;
        }

        private static string SanitizeAssetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Unnamed";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Replace(' ', '_');
        }

        private static List<FlagChange> ParseFlagChanges(string spec)
        {
            var list = new List<FlagChange>();
            if (string.IsNullOrWhiteSpace(spec)) return list;

            // Pattern to match [key:value]
            // key may contain letters, digits, underscores, dashes
            // value: true/false
            var rx = new Regex(@"\[([^\[\]:]+)\s*:\s*(true|false)\]", RegexOptions.IgnoreCase);
            var matches = rx.Matches(spec);
            foreach (Match m in matches)
            {
                var key = m.Groups[1].Value.Trim();
                var valStr = m.Groups[2].Value.Trim();
                if (bool.TryParse(valStr, out var val))
                {
                    list.Add(new FlagChange
                    {
                        flagKey = key,
                        value = val
                    });
                }
                else
                {
                    Debug.LogWarning($"Invalid flag value '{valStr}' for key '{key}' in '{spec}'. Skipping.");
                }
            }
            return list;
        }

        // CSV row models and parsing

        private struct DialogRow
        {
            public string Id;
            public string CharacterId;
            public string DialogText;
            public string NextDialogId;
            public string FlagSpec;

            public static bool TryParse(Dictionary<string, string> row, out DialogRow dr)
            {
                dr = default;
                if (!row.TryGetValue("Dialogue ID", out var id)) return false;

                row.TryGetValue("Character ID", out var characterId);
                row.TryGetValue("Text", out var dialogText);
                row.TryGetValue("Next Dialogue ID", out var nextId);
                row.TryGetValue("Flag", out var flagSpec);

                dr = new DialogRow
                {
                    Id = id?.Trim(),
                    CharacterId = characterId?.Trim(),
                    DialogText = dialogText ?? string.Empty,
                    NextDialogId = string.IsNullOrWhiteSpace(nextId) ? null : nextId.Trim(),
                    FlagSpec = flagSpec?.Trim()
                };
                return true;
            }
        }

        private struct ResponseRow
        {
            public string Id;
            public string DialogId;
            public string Text;
            public string NextDialogId;
            public string FlagSpec;

            public static bool TryParse(Dictionary<string, string> row, out ResponseRow rr)
            {
                rr = default;
                if (!row.TryGetValue("Response ID", out var id)) return false;

                row.TryGetValue("Previous Dialogue ID", out var dialogId);
                row.TryGetValue("Text", out var text);
                row.TryGetValue("Next Dialogue ID", out var nextId);
                row.TryGetValue("Flag", out var flagSpec);

                rr = new ResponseRow
                {
                    Id = id?.Trim(),
                    DialogId = dialogId?.Trim(),
                    Text = text ?? string.Empty,
                    NextDialogId = string.IsNullOrWhiteSpace(nextId) ? null : nextId.Trim(),
                    FlagSpec = flagSpec?.Trim()
                };
                return true;
            }
        }

        private enum ConditionTargetType
        {
            Dialog,
            Response,
            Unknown
        }

        private struct ConditionRow
        {
            public ConditionTargetType TargetType;
            public string TargetTypeRaw;
            public string TargetId;
            public string FlagKey;
            public bool NeededValue;
            public bool DefaultValue; // NOTE: currently unused by Condition struct
            public ConditionScope Scope;

            public static bool TryParse(Dictionary<string, string> row, out ConditionRow cr)
            {
                cr = default;

                if (!row.TryGetValue("Type", out var ttRaw)) return false;
                row.TryGetValue("ID", out var targetId);
                row.TryGetValue("Key", out var flagKey);
                row.TryGetValue("Condition", out var neededValRaw);
                row.TryGetValue("Default Value", out var defaultValRaw);
                row.TryGetValue("Scope", out var scopeRaw);

                var targetType = ParseTargetType(ttRaw);
                var neededVal = ParseBool(neededValRaw, false);
                var defaultVal = ParseBool(defaultValRaw, false);
                var scope = ParseScope(scopeRaw);

                cr = new ConditionRow
                {
                    TargetType = targetType,
                    TargetTypeRaw = ttRaw,
                    TargetId = targetId?.Trim(),
                    FlagKey = flagKey?.Trim(),
                    NeededValue = neededVal,
                    DefaultValue = defaultVal,
                    Scope = scope
                };
                return true;
            }

            private static ConditionTargetType ParseTargetType(string s)
            {
                if (string.IsNullOrEmpty(s)) return ConditionTargetType.Unknown;
                s = s.Trim();
                if (s.Equals("Dialog", StringComparison.OrdinalIgnoreCase)) return ConditionTargetType.Dialog;
                if (s.Equals("Response", StringComparison.OrdinalIgnoreCase)) return ConditionTargetType.Response;
                return ConditionTargetType.Unknown;
            }

            private static bool ParseBool(string s, bool fallback)
            {
                if (string.IsNullOrEmpty(s)) return fallback;
                if (bool.TryParse(s.Trim(), out var v)) return v;
                // Accept 0/1, yes/no
                if (int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) return i != 0;
                if (string.Equals(s.Trim(), "yes", StringComparison.OrdinalIgnoreCase)) return true;
                if (string.Equals(s.Trim(), "no", StringComparison.OrdinalIgnoreCase)) return false;
                return fallback;
            }

            private static ConditionScope ParseScope(string s)
            {
                if (string.IsNullOrEmpty(s)) return ConditionScope.Global;
                s = s.Trim();
                if (s.Equals("Global", StringComparison.OrdinalIgnoreCase)) return ConditionScope.Global;
                if (s.Equals("Loop", StringComparison.OrdinalIgnoreCase)) return ConditionScope.Loop;
                if (s.Equals("Scene", StringComparison.OrdinalIgnoreCase)) return ConditionScope.Scene;
                // Default to Global
                Debug.LogWarning($"Unknown scope '{s}', defaulting to Global.");
                return ConditionScope.Global;
            }
        }

        private static class Csv
        {
            // Reads a CSV file and returns a list of rows as dictionaries (header name -> value).
            // Handles quoted fields and commas within quotes. Trims BOM and whitespace around unquoted fields.
            public static List<Dictionary<string, string>> Read(string path)
            {
                var lines = File.ReadAllLines(path);
                var result = new List<Dictionary<string, string>>();
                if (lines.Length == 0) return result;

                var header = ParseLine(lines[0]);
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var cols = ParseLine(lines[i]);
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int c = 0; c < header.Count; c++)
                    {
                        var key = header[c];
                        var val = c < cols.Count ? cols[c] : string.Empty;
                        if (i == 1 && c == 0) val = TrimBom(val);
                        dict[key] = val;
                    }

                    result.Add(dict);
                }

                return result;
            }

            private static string TrimBom(string s)
            {
                if (string.IsNullOrEmpty(s)) return s;
                return s.TrimStart('\uFEFF', '\u200B');
            }

            // Very small CSV parser supporting:
            // - comma delimiter
            // - double quotes for escaping commas and double quotes ("" -> ")
            private static List<string> ParseLine(string line)
            {
                var list = new List<string>();
                if (line == null) return list;

                int i = 0;
                int len = line.Length;
                while (i <= len)
                {
                    if (i == len)
                    {
                        // end of line: ensure we end after a trailing comma
                        list.Add(string.Empty);
                        break;
                    }

                    if (line[i] == ',')
                    {
                        list.Add(string.Empty);
                        i++;
                        continue;
                    }

                    string value;
                    if (line[i] == '"')
                    {
                        // quoted field
                        i++; // skip opening quote
                        var start = i;
                        var sb = new System.Text.StringBuilder();
                        bool done = false;
                        while (i < len && !done)
                        {
                            if (line[i] == '"')
                            {
                                // possible end or escaped quote
                                if (i + 1 < len && line[i + 1] == '"')
                                {
                                    sb.Append('"');
                                    i += 2;
                                }
                                else
                                {
                                    // end quote
                                    i++;
                                    done = true;
                                }
                            }
                            else
                            {
                                sb.Append(line[i]);
                                i++;
                            }
                        }

                        value = sb.ToString();
                        // skip any spaces until comma or EOL
                        while (i < len && line[i] != ',') i++;
                        if (i < len && line[i] == ',') i++; // skip comma
                    }
                    else
                    {
                        // unquoted field
                        int start = i;
                        while (i < len && line[i] != ',') i++;
                        value = line.Substring(start, i - start).Trim();
                        if (i < len && line[i] == ',') i++; // skip comma
                    }

                    list.Add(value);

                    if (i >= len)
                        break;
                }

                // Remove the extra empty at EOL if no trailing comma
                if (list.Count > 0 && list[^1] == string.Empty && !line.EndsWith(",", StringComparison.Ordinal))
                {
                    list.RemoveAt(list.Count - 1);
                }

                return list;
            }
        }


        #endregion
        }
    }