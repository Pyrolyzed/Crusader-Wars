﻿using Crusader_Wars.twbattle;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Media3D;

namespace Crusader_Wars
{
    /*
     * IMPORTANT NOTE
     * ----------------------------
     * The writter gives some extra new lines '\n'
     * might remove them later
     */
    internal static class Reader
    {
        private static ICharacter Player { get; set; }
        private static ICharacter Enemy { get; set; }

        /// <summary>  
        /// Sets essential data to the Reader. Important to set this before using the Reader!  
        /// </summary>  
        /// <param name="player">Player side object</param>  
        /// <param name="enemy">Enemy side object</param>  
        public static void SetData(ICharacter player, ICharacter enemy)
        {
            Player = player;
            Enemy = enemy;
        }

        /// <summary>  
        /// Reads the ck3 save file for all the needed data.  
        /// </summary>  
        /// <param name="savePath">Path to the ck3 save file</param>  
        public static void ReadFile(string savePath)
        {
            using (FileStream saveFile = File.Open(savePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(saveFile))
            {
                string line = reader.ReadLine();
                while(line != null && !reader.EndOfStream) 
                {
                    line = reader.ReadLine();
                    GetterKeys.ReadProvinceBuildings(line, "5984");
                    GetterKeys.ReadAccolades(line, Player, Enemy);
                    GetterKeys.ReadCourtPositions(line, Player, Enemy);
                    GetterKeys.ReadBattleCharactersTraits(line, Player, Enemy);
                    SearchKeys.TraitsList(line);
                    SearchKeys.Combats(line);
                    SearchKeys.Regiments(line);
                    SearchKeys.ArmyRegiments(line);
                    SearchKeys.Living(line);
                }
                Player = null;
                Enemy = null;

                reader.Close();
                saveFile.Close();
            }

            Data.ConvertDataToString();
            SaveFile.ReadWoundedTraits();
        }


        static bool NeedSkiping { get;set; }
        public static void SendDataToFile(string savePath)
        {

            long startMemory = GC.GetTotalMemory(false);

            string tempFilePath = Directory.GetCurrentDirectory() + "\\CrusaderWars_Battle.ck3";

            using (var inputFileStream = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var streamReader = new StreamReader(inputFileStream))
            using (StreamWriter streamWriter = new StreamWriter(outputFileStream))
            {
                streamWriter.NewLine = "\n";
                string line;
                while ((line = streamReader.ReadLine()) != null || !streamReader.EndOfStream)
                {

                    //Line Skipper
                    if (NeedSkiping && line == "pending_character_interactions={")
                    {
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "\tarmy_regiments={")
                    {
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "\tarmies={")
                    {
                        NeedSkiping = false;
                    }
                    else if (NeedSkiping && line == "dead_unprunable={")
                    {
                        NeedSkiping = false;
                    }


                    if (line == "\tcombats={" && !NeedSkiping)
                    {
                        streamWriter.WriteLine(Data.String_Combats);
                        Console.WriteLine("EDITED COMBATS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "\tregiments={" && !NeedSkiping)
                    {
                        streamWriter.WriteLine(Data.String_Regiments);
                        Console.WriteLine("EDITED REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "\tarmy_regiments={" && !NeedSkiping)
                    {
                        streamWriter.WriteLine(Data.String_ArmyRegiments);
                        Console.WriteLine("EDITED ARMY REGIMENTS SENT!");
                        NeedSkiping = true;
                    }
                    else if (line == "living={" && !NeedSkiping)
                    {
                        streamWriter.WriteLine(Data.String_Living);
                        Console.WriteLine("EDITED LIVING SENT!");
                        NeedSkiping = true;
                    }
                    else if (!NeedSkiping)
                    {
                        streamWriter.WriteLine(line); 
                    }

                }

                streamWriter.Close();
                streamReader.Close();
                outputFileStream.Close();
                inputFileStream.Close();
            }

            string save_games_path = Properties.Settings.Default.VAR_dir_save;
            string editedSavePath = save_games_path + "\\CrusaderWars_Battle.ck3";

            File.Delete(savePath);
            File.Move(tempFilePath, editedSavePath);

            long endMemory = GC.GetTotalMemory(false);
            long memoryUsage = endMemory - startMemory;

            Console.WriteLine($"----\nWritting data to save file\nMemory Usage: {memoryUsage/1048576} megabytes");
        }

    }

    internal static class Data
    {
        public static List<string> PlayerIDsAccolades = new List<string>();
        public static List<string> EnemyIDsAccolades = new List<string>();
        public static List<(string,string,string)> PlayerAccolades = new List<(string,string,string)>();
        public static List<(string,string,string)> EnemysAccolades = new List<(string,string,string)>();

        //Sieges
        public static List<string> Province_Buildings = new List<string>();

        public static StringBuilder Traits = new StringBuilder();
        public static StringBuilder Combats = new StringBuilder();
        public static StringBuilder Living = new StringBuilder();
        public static StringBuilder ArmyRegiments = new StringBuilder();
        public static StringBuilder Regiments = new StringBuilder();

        public static string String_Traits;
        public static string String_Combats;
        public static string String_Living;
        public static string String_ArmyRegiments;
        public static string String_Regiments;

        public static void ConvertDataToString()
        {
            long startMemory = GC.GetTotalMemory(false);

            String_Traits = Traits.ToString();
            String_Combats = Combats.ToString();
            String_Living = Living.ToString();
            String_ArmyRegiments = ArmyRegiments.ToString();
            String_Regiments = Regiments.ToString();

            long endMemory = GC.GetTotalMemory(false);
            long memoryUsage = endMemory - startMemory;

            Console.WriteLine($"----\nConvert all data to string\nMemory Usage: {memoryUsage / 1048576} mb");
        }

        public static void Reset()
        {
            PlayerIDsAccolades = new List<string> ();
            EnemyIDsAccolades = new List<string>();
            PlayerAccolades = new List<(string, string, string)> ();
            EnemysAccolades = new List<(string, string, string)>();

            Traits = new StringBuilder();
            Combats = new StringBuilder();
            Living = new StringBuilder();
            ArmyRegiments = new StringBuilder ();
            Regiments = new StringBuilder ();

            String_Traits = "";
            String_Combats = "";
            String_Living = "";
            String_ArmyRegiments= "";
            String_Regiments = "";

            SearchKeys.HasTraitsExtracted = false;
            SearchKeys.HasCombatsExtracted = false;
            SearchKeys.HasLivingExtracted = false;
            SearchKeys.HasArmyRegimentsExtracted = false;
            SearchKeys.HasRegimentsExtracted = false;
        }
    }

    struct GetterKeys
    {
        static bool isSearchPermitted = false;
        static bool isSearchBuildingsPermitted = false;
        static bool isExtractBuildingsPermitted = false;
        public static void ReadProvinceBuildings(string line, string province_id)
        {
            
            if(line.Contains("provinces={"))
            {
                isSearchPermitted = true;
            }

            
            if(isSearchPermitted && line.Contains($"\t{province_id}={{"))
            {
                isSearchBuildingsPermitted = true;
            }

            if(isSearchBuildingsPermitted && line.Contains("buildings={"))
            {
                isExtractBuildingsPermitted = true;
            }

            if(isExtractBuildingsPermitted)
            {
                if (line.Contains("type="))
                {

                    string building_key = Regex.Match(line, @"=(.+)").Groups[1].Value.Trim('"').Trim('/');
                    Data.Province_Buildings.Add(building_key);
                }


            }


            //last line of the province data
            //stop searching
            if( isSearchBuildingsPermitted && line.Contains("fort_level="))
            {
                string fort_level = Regex.Match(line, @"=(.+)").Groups[1].Value.Trim('"').Trim('/');
                if(int.TryParse(fort_level, out int level))
                {
                    Sieges.SetFortLevel(level);
                }
                else
                {
                    Sieges.SetFortLevel(0);
                }
                
                isExtractBuildingsPermitted = false;
                isSearchBuildingsPermitted = false;
                isSearchPermitted = false;
                return;

            }
        }



        static bool isSearchPermittedAccolades = false;
        static bool StartAccoladeSearchAllowed = false;
        static string found_accolade_id;
        static string primary_attribute;
        static string secundary_attribute;
        static string glory;
        static bool isAccoladePlayer = false;
        static bool isAccoladeEnemy = false;
        public static void ReadAccolades(string line, ICharacter player, ICharacter enemy)
        {
            //if there is a accolade id found
            if(Data.PlayerIDsAccolades.Count > 0 || Data.EnemyIDsAccolades.Count > 0)
            {
                if(line == "accolades={\n")
                {
                    isSearchPermittedAccolades= true;
                }

                //find accolade on the battle
                if(isSearchPermittedAccolades && !StartAccoladeSearchAllowed) 
                {
                    foreach(var id in  Data.PlayerIDsAccolades) 
                    {
                        if (line == $"\t\t{id}={{")
                        {
                            isAccoladePlayer = true;
                            found_accolade_id = id;
                            StartAccoladeSearchAllowed = true;
                            break;
                        }
                    }

                    foreach (var id in Data.EnemyIDsAccolades)
                    {
                        if (line == $"\t\t{id}={{")
                        {
                            isAccoladeEnemy = true;
                            found_accolade_id = id;
                            StartAccoladeSearchAllowed = true;
                            break;
                        }
                    }
                }


                //search for attributes and glory
                if(StartAccoladeSearchAllowed && line.Contains("\t\t\tprimary=")) 
                {
                    primary_attribute = Regex.Match(line, @"\w+", RegexOptions.RightToLeft).Value;
                }
                if (StartAccoladeSearchAllowed && line.Contains("\t\t\tsecondary="))
                {
                    secundary_attribute = Regex.Match(line, @"\w+", RegexOptions.RightToLeft).Value;
                }
                if (StartAccoladeSearchAllowed && line.Contains("\t\t\tglory="))
                {
                    glory = Regex.Match(line, @"\d+", RegexOptions.RightToLeft).Value;
                }


                //add data to list
                if(primary_attribute != String.Empty && secundary_attribute != String.Empty && glory != String.Empty)
                {
                    if(isAccoladePlayer) { Data.PlayerAccolades.Add((primary_attribute, secundary_attribute, glory)); }
                    else { Data.EnemysAccolades.Add((primary_attribute, secundary_attribute, glory)); }
                    
                    primary_attribute = "";
                    secundary_attribute = "";
                    glory = "";
                    isAccoladeEnemy = false;
                    isAccoladePlayer = false;
                }

                //accolade data end line
                if(StartAccoladeSearchAllowed && line == "\t\t}")
                {
                    StartAccoladeSearchAllowed = false;
                    found_accolade_id = "";
                }

                //accolades data group end line
                if(isSearchPermittedAccolades && line == "tax_slot_manager={")
                {
                    player.Knights.SetAccolades(Data.PlayerAccolades);
                    enemy.Knights.SetAccolades(Data.EnemysAccolades);

                    StartAccoladeSearchAllowed = false;
                    isSearchPermittedAccolades = false;


                }


            }
        }

        static bool isSearchPermittedLiving = false;
        static bool StartCharacterSearchAllowed = false;
        static List<(string, int, int, List<string>, BaseSkills, bool)> player_knights_list;
        static List<(string, int, int, List<string>, BaseSkills, bool)> enemy_knights_list;
        static bool isPlayerCommander = false;
        static bool isEnemyCommander = false;
        static bool isPlayerKnight = false;
        static bool isEnemyKnight = false;
        static string char_id = "";
        public static void ReadBattleCharactersTraits(string line, ICharacter Player, ICharacter Enemy)
        {
            if(player_knights_list == null || enemy_knights_list == null)
            {
                player_knights_list = Player.Knights.GetKnightsList();
                enemy_knights_list  = Enemy.Knights.GetKnightsList();
            }

            if (line == "living={")
            {
                isSearchPermittedLiving = true;
            }

            if (isSearchPermittedLiving && !StartCharacterSearchAllowed)
            {
                if(line == ($"\t{Player.Commander.CommanderID}={{") || line == $"{Player.Commander.CommanderID}={{")
                {
                    StartCharacterSearchAllowed = true;
                    isPlayerCommander=true;
                    char_id = Player.Commander.CommanderID;
                }
                else if (line == $"\t{Enemy.Commander.CommanderID}={{" || line == $"{Enemy.Commander.CommanderID}={{")
                {
                    StartCharacterSearchAllowed = true;
                    isEnemyCommander=true;
                    char_id = Enemy.Commander.CommanderID;
                }
                
                foreach(var knight in player_knights_list)
                {
                    if (line == $"\t{knight.Item1}={{" || line == $"{knight.Item1}={{")
                    {
                        StartCharacterSearchAllowed = true;
                        isPlayerKnight=true;
                        char_id = knight.Item1;
                    }
                }
                foreach (var knight in enemy_knights_list)
                {
                    if (line == $"\t{knight.Item1}={{" || line == $"{knight.Item1}={{")
                    {
                        StartCharacterSearchAllowed = true;
                        isEnemyKnight=true;
                        char_id = knight.Item1;
                    }
                }
            }

            if(StartCharacterSearchAllowed)
            {
                if(line.Contains("\ttraits={"))
                {
                    var traits_collection = Regex.Matches(line, @"\d+").Cast<Match>()
                                                                        .Select(m => m.Value)
                                                                        .ToList<string>();
                    
                    if (isPlayerCommander)
                    {
                        Player.Commander.SetTraits(traits_collection);
                    }
                    else if(isEnemyCommander)
                    {
                        Enemy.Commander.SetTraits(traits_collection);
                    }
                    else if(isPlayerKnight)
                    {
                        Player.Knights.SetTraits(char_id, traits_collection);
                    }
                    else if(isEnemyKnight)
                    {
                        Enemy.Knights.SetTraits(char_id, traits_collection);
                    }

                }


                if(line.Contains("\tskill={"))
                {
                    var skills_collection = Regex.Matches(line, @"\d+").Cast<Match>()
                                                .Select(m => m.Value)
                                                .ToList<string>();

                    if (isPlayerKnight)
                    {
                        BaseSkills skills = new BaseSkills(skills_collection);
                        Player.Knights.SetSkills(char_id, skills);
                    }
                    else if (isEnemyKnight)
                    {
                        BaseSkills skills = new BaseSkills(skills_collection);
                        Enemy.Knights.SetSkills(char_id, skills);
                    }
                }

                //set the knight as accolade if true and add id to a list
                if (line.Contains("\t\taccolade="))
                {
                    var player_knights = Player.Knights.GetKnightsList();
                    var enemy_knights = Enemy.Knights.GetKnightsList();

                    (string, int, int, List<string>, BaseSkills, bool) accolade_knight;
                    try { accolade_knight = player_knights.First(x => x.Item1 == char_id);  }
                    catch { accolade_knight = enemy_knights.First(x => x.Item1 == char_id); }


                    if (isPlayerKnight)
                    {
                        int index = player_knights.IndexOf(accolade_knight);
                        accolade_knight = ((accolade_knight.Item1, 7, accolade_knight.Item3, accolade_knight.Item4, accolade_knight.Item5, true));
                        player_knights[index] = accolade_knight;
                    }
                    else if (isEnemyKnight)
                    {
                        int index = enemy_knights.IndexOf(accolade_knight);
                        accolade_knight = ((accolade_knight.Item1, 7, accolade_knight.Item3, accolade_knight.Item4, accolade_knight.Item5,true));
                        enemy_knights[index] = accolade_knight;
                    }


                    string accolade_id = Regex.Match(line, @"\d+").Value;
                    if (isPlayerKnight) { Data.PlayerIDsAccolades.Add(accolade_id); }
                    else if(isEnemyKnight) { Data.EnemyIDsAccolades.Add(accolade_id); }

                }


            }

            //end line to specific court position data
            if (StartCharacterSearchAllowed && line == "}")
            {
                isEnemyCommander = false;
                isPlayerCommander = false;
                isEnemyKnight = false;
                isPlayerKnight = false;
                char_id = "";
                StartCharacterSearchAllowed = false;
            }

            //end line to all court positions data
            if(isSearchPermittedLiving && line == "dead_unprunable={")
            {
                player_knights_list = new List<(string, int, int, List<string>, BaseSkills, bool)>();
                enemy_knights_list = new List<(string, int, int, List<string>, BaseSkills, bool)> ();
                isEnemyCommander = false;
                isPlayerCommander = false;
                isEnemyKnight = false;
                isPlayerKnight = false;
                char_id = "";
                StartCharacterSearchAllowed = false;
                isSearchPermittedLiving = false;
            }
        }

        static bool isSearchPermittedCourtPositions = false;
        static bool StartCourtPositionsSearchAllowed = false;
        static string employee;
        static string profession;
        public static void ReadCourtPositions(string line, ICharacter Player, ICharacter Enemy)
        {
            if (line == "court_positions={")
            {
                isSearchPermittedCourtPositions = true;
            }

            if (isSearchPermittedCourtPositions && !StartCourtPositionsSearchAllowed)
            {
                if (line == "\t\t\tcourt_position=\"bodyguard_court_position\"")
                {
                    profession = "bodyguard";
                    StartCourtPositionsSearchAllowed = true;
                }
                else if (line == "\t\t\tcourt_position=\"champion_court_position\"")
                {
                    profession = "personal_champion";
                    StartCourtPositionsSearchAllowed = true;
                }

            }

            if (StartCourtPositionsSearchAllowed)
            {
                if (line.Contains("\t\t\temployee="))
                {
                    employee = Regex.Match(line, @"=(.+)").Groups[1].Value;
                }


                if (line == $"\t\t\temployer={Player.ID}")
                {
                    Player.Commander.AddCourtPosition(profession, employee);
                }

                if (line == $"\t\t\temployer={Enemy.ID}")
                {
                    Enemy.Commander.AddCourtPosition(profession, employee);
                }
            }

            //end line to specific court position data
            if (StartCourtPositionsSearchAllowed && line == "\t\t}")
            {
                employee = "";
                profession = "";
                StartCourtPositionsSearchAllowed = false;
            }

            //end line to all court positions data
            if (isSearchPermittedCourtPositions && line == "}")
            {
                employee = "";
                profession = "";
                StartCourtPositionsSearchAllowed = false;
                isSearchPermittedCourtPositions = false;
            }
        }

    };

    struct SearchKeys
    {
        private static bool Start_TraitsFound { get; set; }
        private static bool End_TraitsFound { get; set; }
        public static bool HasTraitsExtracted { get; set; }

        public static void TraitsList(string line)
        {
            if (!HasTraitsExtracted)
            {
                if (!Start_TraitsFound)
                {
                    if (line.Contains("traits_lookup={"))
                    {
                        Start_TraitsFound = true; Console.WriteLine("TRAITS START KEY FOUND!");
                    }
                    else { Start_TraitsFound = false; }
                }

                if (Start_TraitsFound && !End_TraitsFound)
                {
 
                    if (line == "provinces={")
                    {
                        End_TraitsFound = true;
                        Console.WriteLine("TRAITS END KEY FOUND!");
                        return;
                    }
                    else { End_TraitsFound = false; }

                    Data.Traits.Append(line + "\n");

                }

                if (End_TraitsFound)
                {
                    HasTraitsExtracted = true;
                    Start_TraitsFound = false;
                    End_TraitsFound = false;
                }
            }
        }

        private static bool Start_CombatsFound { get; set; }
        private static bool End_CombatsFound { get; set; }
        public static bool HasCombatsExtracted { get; set; }

        public static void Combats(string line)
        {
            if(!HasCombatsExtracted)
            {
                if (!Start_CombatsFound)
                {
                    if (line == "\tcombats={") { 
                        Start_CombatsFound = true; Console.WriteLine("COMBAT START KEY FOUND!"); 
                    }
                    else { Start_CombatsFound = false; }
                }

                if(Start_CombatsFound && !End_CombatsFound)
                {
                    //Match end = Regex.Match(line, @"pending_character_interactions={");
                    if (line == "pending_character_interactions={") 
                    {
                        End_CombatsFound = true; 
                        Console.WriteLine("COMBAT END KEY FOUND!");
                        return;
                    }
                    else { End_CombatsFound = false; }

                    Data.Combats.Append(line + "\n");
                    
                }

                if(End_CombatsFound)
                {
                    HasCombatsExtracted = true;
                    Start_CombatsFound = false; 
                    End_CombatsFound = false;
                }
            }
        }

        private static bool Start_RegimentsFound { get; set; }
        private static bool End_RegimentsFound { get; set; }
        public static bool HasRegimentsExtracted { get; set; }
        public static void Regiments(string line)
        {
            if (!HasRegimentsExtracted)
            {
                if (!Start_RegimentsFound)
                {
                    if (line == "\tregiments={") { Start_RegimentsFound = true; Console.WriteLine("REGIMENTS START KEY FOUND!"); }
                    else { Start_RegimentsFound = false; }
                }

                if (Start_RegimentsFound && !End_RegimentsFound)
                {
                  
                    if (line == "\tarmy_regiments={") 
                    { 
                        End_RegimentsFound = true; 
                        Console.WriteLine("REGIMENTS END KEY FOUND!");
                        return;
                    }
                    else { End_RegimentsFound = false; }

                    Data.Regiments.Append(line + "\n");
                }

                if (End_RegimentsFound)
                {
                    HasRegimentsExtracted = true;
                    Start_RegimentsFound = false; 
                    End_RegimentsFound = false;
                    //Console.WriteLine(Data.Regiments);
                }
            }
        }

        private static bool Start_ArmyRegimentsFound { get; set; }
        private static bool End_ArmyRegimentsFound { get; set; }
        public static bool HasArmyRegimentsExtracted { get; set; }
        public static void ArmyRegiments(string line)
        {
            if (!HasArmyRegimentsExtracted)
            {
                if (!Start_ArmyRegimentsFound)
                {
                    if (line == "\tarmy_regiments={") { Start_ArmyRegimentsFound = true; Console.WriteLine("ARMY REGIMENTS START KEY FOUND!"); }
                    else { Start_ArmyRegimentsFound = false; }
                }

                if (Start_ArmyRegimentsFound && !End_ArmyRegimentsFound)
                {
                    
                    if (line == "\tarmies={") 
                    { 
                        End_ArmyRegimentsFound = true; 
                        Console.WriteLine("ARMY REGIMENTS END KEY FOUND!");
                        return;
                    }
                    else { End_ArmyRegimentsFound = false; }

                    Data.ArmyRegiments.Append(line + "\n");
                }

                if (End_ArmyRegimentsFound)
                {
                    HasArmyRegimentsExtracted = true;
                    Start_ArmyRegimentsFound = false;
                    End_ArmyRegimentsFound = false;
                    //Console.WriteLine(Data.ArmyRegiments);
                }
            }
        }

        private static bool Start_LivingFound { get; set; }
        private static bool End_LivingFound { get; set; }
        public static bool HasLivingExtracted { get; set; }
        public static void Living(string line)
        {
            if (!HasLivingExtracted)
            {
                if (!Start_LivingFound)
                {
                    //Match start = Regex.Match(line, @"living={");
                    if (line == "living={") { Start_LivingFound = true; Console.WriteLine("LIVING START KEY FOUND!"); }
                    else { Start_LivingFound = false; }
                }

                if (Start_LivingFound && !End_LivingFound)
                {
                    if (line == "dead_unprunable={") 
                    { 
                        End_LivingFound = true; 
                        Console.WriteLine("LIVING END KEY FOUND!");
                        return;
                    }
                    else { End_LivingFound = false; }

                    Data.Living.Append(line + "\n");
                }

                if (End_LivingFound)
                {
                    HasLivingExtracted = true;
                    Start_LivingFound = false;
                    End_LivingFound = false;
                }
            }
        }
    }
}
