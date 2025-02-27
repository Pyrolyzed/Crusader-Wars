﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;

namespace Crusader_Wars.data.save_file
{


    public static class ArmiesReader
    {

        // V1.0 Beta
        static List<Army> attacker_armies;
        static List<Army> defender_armies;
        public static List<(string name, int index)> save_file_traits { get; set; }
        public static void ReadCombats(string g)
        {
            ReadCombatArmies(g);
        }
        public static (List<Army> attacker, List<Army> defender) ReadBattleArmies()
        {
            ReadSaveFileTraits();

            ReadArmiesData();
            ReadArmiesUnits();
            ReadArmyRegiments();
            ReadCombatSoldiersNum(BattleResult.Player_Combat);
            ReadRegiments();
            ReadOriginsKeys();

            LandedTitles.ReadProvinces(attacker_armies, defender_armies);
            ReadCountiesManager();
            ReadMercenaries();
            BattleFile.SetArmiesSides(attacker_armies, defender_armies);

            CreateKnights();
            CreateMainCommanders();
            ReadCharacters();
            ReadCourtPositions();
            CheckForNullCultures();
            ReadCultureManager();



            // Organize Units
            CreateUnits();

            // Print Armies
            Print.PrintArmiesData(attacker_armies);
            Print.PrintArmiesData(defender_armies);

            return (attacker_armies, defender_armies);
        }


        static void ClearNullArmyRegiments()
        {
            // Clear Empty Regiments
            for (int i = 0; i < attacker_armies.Count; i++)
            {
                attacker_armies[i].ClearNullArmyRegiments();
            }
            for (int i = 0; i < defender_armies.Count; i++)
            {
                defender_armies[i].ClearNullArmyRegiments();
            }
        }

        static void CheckForNullCultures()
        {
            Console.WriteLine("ATTACKER  WITH NULL CULTURE REGIMENTS:\n");
            foreach(Regiment regiment in attacker_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                Console.WriteLine($"WARNING - REGIMENT {regiment.ID} HAS A NULL CULTURE");
            }

            Console.WriteLine("DEFENDER  WITH NULL CULTURE REGIMENTS:\n");
            foreach (Regiment regiment in defender_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiment => armyRegiment.Regiments))
            {
                Console.WriteLine($"WARNING - REGIMENT {regiment.ID} HAS A NULL CULTURE");
            }
        }

        static void ReadSaveFileTraits()
        {
            MatchCollection allTraits = Regex.Matches(File.ReadAllText(Writter.DataFilesPaths.Traits_Path()), @" (\w+)");
            save_file_traits = new List<(string name, int index)>();

            for (int i = 0; i < allTraits.Count; i++)
            {
                //save_file_traits[i] = (allTraits[i].Groups[1].Value, i);
                save_file_traits.Add((allTraits[i].Groups[1].Value, i));
            }
        }
         
        public static int GetTraitIndex(string trait_name)
        {
            int index;
            index = save_file_traits.FirstOrDefault(x => x.name == trait_name).index;
            return index;

        }

        public static string GetTraitKey(int trait_index)
        {
            string key;
            key = save_file_traits.FirstOrDefault(x => x.index == trait_index).name;
            return key;

        }

        static void ReadCourtPositions()
        {
            string profession="";
            string employeeID="";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.CourtPositions_Path()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (line == "\t\t\tcourt_position=\"bodyguard_court_position\"")
                    {
                        profession = "bodyguard";
                    }
                    else if (line == "\t\t\tcourt_position=\"champion_court_position\"")
                    {
                        profession = "personal_champion";
                    }
                    else if (line == "\t\t\tcourt_position=\"garuva_warrior_court_position\"")
                    {
                        profession = "garuva_warrior";
                    }
                    else if (line.Contains("\t\t\temployee="))
                    {
                        employeeID = Regex.Match(line, @"\d+").Groups[1].Value;
                    }
                    else if (line.StartsWith("\t\t\temployer="))
                    {
                        string employerID = Regex.Match(line, @"\d+").Value;

                        var army = attacker_armies.Find(x => x.CommanderID == employerID)
                                   ?? defender_armies.Find(x => x.CommanderID == employerID) ?? null;

                        if (army != null)
                        {
                            army.Commander?.AddCourtPosition(profession, employeeID);
                        }

                    }
                }
            }
        }


        static void ReadCharacters()
        {
            bool searchStarted = false;
            bool isKnight = false, isCommander = false, isMainCommander = false, isOwner = false;
            Army searchingArmy = null;
            Knight searchingKnight = null;

            //non-main army commander variables
            int nonMainCommander_Rank = 1;
            string nonMainCommander_Name="";
            BaseSkills nonMainCommander_BaseSkills = null;
            Culture nonMainCommander_Culture = null;
            Accolade nonMainCommander_Accolade = null;
            int nonMainCommander_Prowess = 0;
            List<(int index, string key)> nonMainCommander_Traits = null;



            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                string line;
                while((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (Regex.IsMatch(line, @"\d+={") && !searchStarted)
                    {
                        string line_id = Regex.Match(line, @"(\d+)={").Groups[1].Value;

                        var searchingData = Armies_Functions.SearchCharacters(line_id, attacker_armies);
                        if (searchingData.searchStarted)
                        {
                            searchStarted = true;
                            isKnight = searchingData.isKnight;
                            isMainCommander = searchingData.isMainCommander;
                            isCommander = searchingData.isCommander;
                            isOwner = searchingData.isOwner;
                            searchingArmy = searchingData.searchingArmy;
                            searchingKnight = searchingData.knight;

                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchCharacters(line_id, defender_armies);
                            if (searchingData.searchStarted)
                            {
                                searchStarted = true;
                                isKnight = searchingData.isKnight;
                                isMainCommander = searchingData.isMainCommander;
                                isCommander = searchingData.isCommander;
                                isOwner = searchingData.isOwner;
                                searchingArmy = searchingData.searchingArmy;
                                searchingKnight = searchingData.knight;
                            }
                        }
                    }
                    else if (searchStarted && line.StartsWith("\tfirst_name=")) //# FIRST NAME
                    {
                        if(isCommander)
                        {
                            nonMainCommander_Name = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        }
                    }
                    else if (searchStarted && line.StartsWith("\tskill={")) //# BASE SKILLS
                    {
                        MatchCollection found_skills = Regex.Matches(line, @"\d+");
                        var baseSkills_list = new List<string>();
                        baseSkills_list = found_skills.Cast<Match>().Select(m => m.Value).ToList();

                        if (isMainCommander)
                        {
                            searchingArmy.Commander.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_BaseSkills = new BaseSkills(baseSkills_list);
                        }
                        else if(isKnight)
                        {
                            searchingKnight.SetBaseSkills(new BaseSkills(baseSkills_list));
                        }
                    }
                    else if(searchStarted && line.StartsWith("\t\taccolade=")) // # ACCOLADE
                    {
                        string accoladeID = Regex.Match(line, @"\d+").Value;
                        if(isKnight)
                        {
                            searchingKnight.IsAccolade(true, GetAccolade(accoladeID));
                        }
                        else if(isMainCommander)
                        {
                            searchingArmy.Commander.SetAccolade(GetAccolade(accoladeID));
                        }
                        else if(isCommander)
                        {
                            nonMainCommander_Accolade = GetAccolade(accoladeID);
                        }
                    }
                    else if (searchStarted && line.StartsWith("\ttraits={")) //# TRAITS
                    {
                        MatchCollection found_traits = Regex.Matches(line, @"\d+");
                        var traits_list = new List<(int index, string key)>();
                        foreach (Match found_trait in found_traits)
                        {
                            int index = Int32.Parse(found_trait.Value);
                            string key = GetTraitKey(index);
                            traits_list.Add((index, key));
                        }

                        if (isMainCommander)
                        {
                            searchingArmy.Commander.SetTraits(traits_list);
                        }
                        else if (isCommander)
                        {
                            nonMainCommander_Traits = traits_list;
                        }
                        else if (isKnight)
                        {
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight).SetTraits(traits_list);
                            searchingArmy.Knights.GetKnightsList().FirstOrDefault(x => x == searchingKnight).SetWoundedDebuffs();
                        }
                    }
                    else if (searchStarted && line.Contains("\tculture=")) //# CULTURE
                    {
                        string culture_id = Regex.Match(line, @"\d+").Value;
                        if (isKnight)
                        {
                            searchingArmy.Knights.GetKnightsList().Find(x => x == searchingKnight).ChangeCulture(new Culture(culture_id));
                            searchingArmy.Knights.SetMajorCulture();
                            if(isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }

                        else if (isMainCommander)
                        {
                            if(searchingArmy.IsPlayer())
                                searchingArmy.Commander.ChangeCulture(new Culture(CK3LogData.LeftSide.GetCommander().culture_id));
                            else
                                searchingArmy.Commander.ChangeCulture(new Culture(CK3LogData.RightSide.GetCommander().culture_id));
                            if (isOwner)
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                            /*
                            searchingArmy.Commander.ChangeCulture(new Culture(culture_id));
                            if (isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                            */
                        }
                        else if(isCommander)
                        {
                            nonMainCommander_Culture = new Culture(culture_id);
                            if (isOwner) 
                                searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }
                        else
                        {
                            searchingArmy.Owner.SetCulture(new Culture(culture_id));
                        }


                    }
                    else if (searchStarted && line.Contains("\t\tdomain={")) //# TITLES
                    {
                        string firstTitleID = Regex.Match(line, @"\d+").Value;
                        if (isCommander)
                        {
                            if (isOwner) searchingArmy.Owner.SetPrimaryTitle(GetTitleKey(firstTitleID));

                            var landedTitlesData = GetCommanderNobleRankAndTitleName(firstTitleID);
                            nonMainCommander_Rank = landedTitlesData.rank;
                            if (searchingArmy.IsPlayer())
                            {
                                if (CK3LogData.LeftSide.GetKnights().Exists(x => x.id == searchingArmy.CommanderID))
                                {
                                    var commanderKnight = CK3LogData.LeftSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                    nonMainCommander_Prowess = Int32.Parse(commanderKnight.prowess);
                                    if (nonMainCommander_Rank == 1)
                                        nonMainCommander_Name = commanderKnight.name;
                                    else
                                        nonMainCommander_Name = $"{commanderKnight.name} of {landedTitlesData.titleName}";
                                }
                                else
                                {
                                    nonMainCommander_Prowess = nonMainCommander_BaseSkills.prowess;
                                    if (nonMainCommander_Rank > 1)
                                        nonMainCommander_Name += $" of {landedTitlesData.titleName}";
                                }

                            }
                            else
                            {
                                if (CK3LogData.RightSide.GetKnights().Exists(x => x.id == searchingArmy.CommanderID))
                                {
                                    var commanderKnight = CK3LogData.RightSide.GetKnights().FirstOrDefault(x => x.id == searchingArmy.CommanderID);
                                    nonMainCommander_Prowess = Int32.Parse(commanderKnight.prowess);
                                    if (nonMainCommander_Rank == 1)
                                        nonMainCommander_Name = commanderKnight.name;
                                    else
                                        nonMainCommander_Name = $"{commanderKnight.name} of {landedTitlesData.titleName}";
                                }
                                else
                                {
                                    nonMainCommander_Prowess = nonMainCommander_BaseSkills.prowess;
                                    if (nonMainCommander_Rank > 1)
                                        nonMainCommander_Name += $" of {landedTitlesData.titleName}";
                                }

                            }
                        }
                        else if(isOwner) // <-- Owner
                        {
                            searchingArmy.Owner.SetPrimaryTitle(GetTitleKey(firstTitleID));
                        }
                    }
                    else if (searchStarted && line == "}")
                    {
                        if (isCommander)
                        {
                            searchingArmy.SetCommander(new CommanderSystem(nonMainCommander_Name, searchingArmy.CommanderID, nonMainCommander_Prowess, nonMainCommander_Rank, nonMainCommander_BaseSkills, nonMainCommander_Culture));
                            searchingArmy.Commander.SetTraits(nonMainCommander_Traits);
                            if (nonMainCommander_Accolade != null) searchingArmy.Commander.SetAccolade(nonMainCommander_Accolade);
                        }

                        searchStarted = false;
                        isCommander = false;
                        isMainCommander = false;
                        isOwner = false;
                        isKnight = false;
                        searchingKnight = null;
                        searchingArmy = null;

                        nonMainCommander_Rank = 1;
                        nonMainCommander_Name = "";
                        nonMainCommander_BaseSkills = null;
                        nonMainCommander_Culture = null;
                        nonMainCommander_Traits = null;
                        nonMainCommander_Prowess = 0;
                    }
                }
            }
        }


        static Accolade GetAccolade(string accoladeID)
        {
            bool searchStarted = false;
            string primaryAttribute = "";
            string secundaryAttribute = "";
            string glory = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Accolades()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (!searchStarted && line == $"\t\t{accoladeID}={{")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tprimary="))
                    {
                        primaryAttribute = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tsecundary="))
                    {
                        secundaryAttribute = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                    }
                    else if (searchStarted && line.StartsWith("\t\t\tglory="))
                    {
                        glory = Regex.Match(line, @"\d+").Value;
                    }
                    else if (searchStarted && line == "\t\t}")
                    {
                        searchStarted = false;
                        return new Accolade(accoladeID, primaryAttribute, secundaryAttribute, glory);
                    }
                }
            }

            return null;
        }

        static string GetTitleKey(string title_id)
        {
            bool searchStarted = false;
            string titleKey = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (line == $"{title_id}={{")
                    {
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        titleKey = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        return titleKey;
                    }

                }
                return titleKey;
            }
        }

        static void ReadOriginsKeys()
        {
            bool searchStarted = false;
            string originKey = "";
            string id = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string line;
                string title_id = "";
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if (!line.StartsWith("\t") && line != "}")
                    {
                        id = Regex.Match(line, @"\d+").Value;
                        searchStarted = true;
                    }
                    else if (searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        originKey = Regex.Match(line, "=(.+)").Groups[1].Value;
                        SetRegimentsOriginsKeys(id,originKey);
                    }
                    else if (searchStarted && line == "}")
                    {
                        searchStarted= false;
                        title_id = "";
                        originKey = "";
                        id = "";
                    }

                }
            }
        }

        static void SetRegimentsOriginsKeys(string title_id, string originKey)
        {
            foreach (Regiment regiment in attacker_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiments => armyRegiments.Regiments))
            {
                if(!string.IsNullOrEmpty(regiment.OwningTitle) && string.IsNullOrEmpty(regiment.OriginKey))
                {
                    if (regiment.OwningTitle == title_id)
                    {
                        regiment.SetOriginKey(originKey);
                    }
                }
            }

            foreach (Regiment regiment in defender_armies.SelectMany(army => army.ArmyRegiments).SelectMany(armyRegiments => armyRegiments.Regiments))
            {
                if (!string.IsNullOrEmpty(regiment.OwningTitle) && string.IsNullOrEmpty(regiment.OriginKey))
                {
                    if(regiment.OwningTitle == title_id)
                    {
                        regiment.SetOriginKey(originKey);
                    }

                }
            }
        }

        static (int rank, string titleName) GetCommanderNobleRankAndTitleName(string commanderTitleID)
        {
            bool searchStarted = false;
            int rankInt = 1; string titleName = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.LandedTitles()))
            {
                string line;
                while ((line = sr.ReadLine()) != null && !sr.EndOfStream)
                {
                    if(line == $"{commanderTitleID}={{")
                    {
                        searchStarted = true;
                    }
                    else if(searchStarted && line.StartsWith("\tkey=")) //# KEY
                    {
                        string title_key = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        
                        if(title_key.StartsWith("b_"))
                        {
                            rankInt = 2;
                        }
                        else if (title_key.StartsWith("c_"))
                        {
                            rankInt = 3;
                        }
                        else if (title_key.StartsWith("d_"))
                        {
                            rankInt = 4;
                        }
                        else if (title_key.StartsWith("k_"))
                        {
                            rankInt = 5;
                        }
                        else if (title_key.StartsWith("e_"))
                        {
                            rankInt = 6;
                        }
                    }
                    else if(searchStarted && line.StartsWith("\tname="))
                    {
                        string name = Regex.Match(line, "\"(.+)\"").Groups[1].Value;
                        titleName = name;

                        return (rankInt, titleName);
                    }
                }
                return (1, string.Empty);
            }
        }

        static void RemoveCommandersAsKnights()
        {
            foreach(Army army in attacker_armies)
            {
                ArmyRegiment commanderRegiment = army.ArmyRegiments.FirstOrDefault(x => x.MAA_Name == army.CommanderID) ?? null;
                if(commanderRegiment != null)
                    army.ArmyRegiments.Remove(commanderRegiment);
            }
            foreach (Army army in defender_armies)
            {
                ArmyRegiment commanderRegiment = army.ArmyRegiments.FirstOrDefault(x => x.MAA_Name == army.CommanderID) ?? null;
                if (commanderRegiment != null)
                    army.ArmyRegiments.Remove(commanderRegiment);
            }
        }
        
        public static List<Army> GetSideArmies(string side)
        {
            List<Army> left_side = null, right_side = null;
            foreach (var army in attacker_armies)
            {
                if (army.IsPlayer())
                {
                    left_side = attacker_armies;
                    break;
                }
                else if (army.IsEnemy())
                {
                    right_side = attacker_armies;
                    break;
                }
            }
            foreach (var army in defender_armies)
            {
                if (army.IsPlayer())
                {
                    left_side = defender_armies;
                    break;
                }
                else if (army.IsEnemy())
                {
                    right_side = defender_armies;
                    break;
                }
            }

            if (side == "left")
                return left_side;
            else
                return right_side;
        }

        static void CreateMainCommanders()
        {
            var left_side_armies = GetSideArmies("left");
            var right_side_armies = GetSideArmies("right");

            var left_main_commander_data = CK3LogData.LeftSide.GetCommander();
            left_side_armies.First(x => x.isMainArmy).SetCommander(new CommanderSystem(left_main_commander_data.name, left_main_commander_data.id, left_main_commander_data.prowess, left_main_commander_data.martial, left_main_commander_data.rank, true));

            var right_main_commander_data = CK3LogData.RightSide.GetCommander();
            right_side_armies.First(x => x.isMainArmy).SetCommander(new CommanderSystem(right_main_commander_data.name, right_main_commander_data.id, right_main_commander_data.prowess, right_main_commander_data.martial, right_main_commander_data.rank, true));
        }
        static void CreateKnights()
        {
            RemoveCommandersAsKnights();

            var left_side_armies = GetSideArmies("left");
            var right_side_armies = GetSideArmies("right");

            


            var KnightsList = new List<Knight>();
            for (int x = 0; x < left_side_armies.Count; x++)
            {
                var army = left_side_armies[x];
                for (int y = 0; y< army.ArmyRegiments.Count;y++)
                {
                    var regiment = army.ArmyRegiments[y];
                    if(regiment.Type == RegimentType.Knight)
                    {
                        for (int i = 0; i < CK3LogData.LeftSide.GetKnights().Count; i++)
                        {
                            string id = CK3LogData.LeftSide.GetKnights()[i].id;
                            if (id == army.CommanderID) continue;
                            if (id == regiment.MAA_Name)
                            {
                                int prowess = Int32.Parse(CK3LogData.LeftSide.GetKnights()[i].prowess);
                                string name = CK3LogData.LeftSide.GetKnights()[i].name;

                                KnightsList.Add(new Knight(name, regiment.MAA_Name, null, prowess, 4));
                            }
                        }
                    }

                }

                int leftEffectivenss = 0;
                if (CK3LogData.LeftSide.GetKnights() is null || CK3LogData.LeftSide.GetKnights().Count == 0)
                    leftEffectivenss = 0;
                else
                    leftEffectivenss = CK3LogData.LeftSide.GetKnights()[0].effectiveness;
                if (CK3LogData.LeftSide.GetKnights().Count > 0)
                {
                    leftEffectivenss = CK3LogData.LeftSide.GetKnights()[0].effectiveness;
                }
                
                KnightSystem leftSide = new KnightSystem(KnightsList, leftEffectivenss);
                if(left_side_armies == attacker_armies)
                {
                    attacker_armies[x].SetKnights(leftSide);
                }
                else if(left_side_armies == defender_armies)
                {
                    defender_armies[x].SetKnights(leftSide);
                }
                KnightsList = new List<Knight>();
                
            }


            KnightsList = new List<Knight>();
            for (int x = 0; x < right_side_armies.Count; x++)
            {
                var army = right_side_armies[x];
                for (int y = 0; y < army.ArmyRegiments.Count; y++)
                {
                    var regiment = army.ArmyRegiments[y];
                    if(regiment.Type == RegimentType.Knight)
                    {
                        for (int i = 0; i < CK3LogData.RightSide.GetKnights().Count; i++)
                        {
                            string id = CK3LogData.RightSide.GetKnights()[i].id;
                            if (id == army.CommanderID) continue;
                            if (id == regiment.MAA_Name)
                            {
                                int prowess = Int32.Parse(CK3LogData.RightSide.GetKnights()[i].prowess);
                                string name = CK3LogData.RightSide.GetKnights()[i].name;

                                KnightsList.Add(new Knight(name, regiment.MAA_Name, null, prowess, 4));
                            }
                        }
                    }

                }

                int rightEffectivenss = 0;
                if (CK3LogData.RightSide.GetKnights() is null || CK3LogData.RightSide.GetKnights().Count == 0)
                    rightEffectivenss = 0;
                else
                    rightEffectivenss = CK3LogData.RightSide.GetKnights()[0].effectiveness;

                KnightSystem rightSide = new KnightSystem(KnightsList, rightEffectivenss);

                if (right_side_armies == attacker_armies)
                {
                    attacker_armies[x].SetKnights(rightSide);
                }
                else if (right_side_armies == defender_armies)
                {
                    defender_armies[x].SetKnights(rightSide);
                }
                KnightsList = new List<Knight>();
            }
        }

        private static void CreateUnits()
        {
            Armies_Functions.CreateUnits(attacker_armies);
            Armies_Functions.CreateUnits(defender_armies);
        }

        private static void ReadMercenaries()
        {
            bool isSearchStarted = false;
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Mercenaries_Path()))
            {
                string culture_id = "";
                List<string> regiments_ids = new List<string>();

                while(true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;

                    //Mercenary Company ID
                    if(Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        isSearchStarted = true;
                        continue;
                    }
                    else if(line == "\t\t}")
                    {
                        var attacker_mercenaries_regiments = attacker_armies.SelectMany(army => army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments))
                                                            .Where(regiment => regiment.isMercenary())
                                                            .ToList();

                        
                        var defender_mercenaries_regiments = defender_armies.SelectMany(army => army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments))
                                                            .Where(regiment => regiment.isMercenary())
                                                            .ToList();

                        //break loop if all cultures are set
                        int attackerNumOfNotSetCultures = attacker_mercenaries_regiments.Count(x => x.Culture == null);
                        int defenderNumOfNotSetCultures = defender_mercenaries_regiments.Count(x => x.Culture == null);
                        if (attackerNumOfNotSetCultures == 0 && defenderNumOfNotSetCultures == 0)
                            break;


                        for (int i = 0; i < attacker_armies.Count; i++)
                        {
                            //Army Regiments
                            for (int x = 0; x < attacker_armies[i].ArmyRegiments.Count; x++)
                            {
                                //Regiments
                                if(attacker_armies[i].ArmyRegiments[x].Regiments != null)
                                {
                                    for (int y = 0; y < attacker_armies[i].ArmyRegiments[x].Regiments.Count; y++)
                                    {
                                        var regiment = attacker_armies[i].ArmyRegiments[x].Regiments[y];

                                        foreach (var t in regiments_ids)
                                        {

                                            if (t == regiment.ID && (regiment.isMercenary() || regiment.Culture is null))
                                            {
                                                attacker_armies[i].ArmyRegiments[x].Regiments[y].SetCulture(culture_id);
                                                break;
                                            }
                                        }

                                    }
                                }

                            }
                        }

                        for (int i = 0; i < defender_armies.Count; i++)
                        {
                            //Army Regiments
                            for (int x = 0; x < defender_armies[i].ArmyRegiments.Count; x++)
                            {
                                //Regiments
                                if(defender_armies[i].ArmyRegiments[x].Regiments != null)
                                {
                                    for (int y = 0; y < defender_armies[i].ArmyRegiments[x].Regiments.Count; y++)
                                    {
                                        var regiment = defender_armies[i].ArmyRegiments[x].Regiments[y];
                                        foreach (var t in regiments_ids)
                                        {
                                            if (t == regiment.ID && (regiment.isMercenary() || regiment.Culture is null))
                                            {
                                                defender_armies[i].ArmyRegiments[x].Regiments[y].SetCulture(culture_id);
                                                break;
                                            }
                                        }
                                    }
                                }

                            }
                        }

                    }
                    else if (isSearchStarted)
                    {
                        if (line.Contains("\t\tculture="))
                        {
                            culture_id = Regex.Match(line, @"\d+").Value;
                        }
                        else if (line.Contains("\t\tregiments={ "))
                        {
                            regiments_ids = Regex.Matches(line, @"\d+").Cast<Match>().Select(match => match.Value).ToList();
                        }
                    }

                }
            }

            //HOLY ORDER REGIMENTS
            
            foreach(var army in attacker_armies)
            {
                var attacker_holyorder_regiments = army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments)
                                              .Where(regiment => regiment.isMercenary() && regiment.Culture == null);
                foreach(var holy_regiment in attacker_holyorder_regiments)
                {
                    holy_regiment.SetCulture(army.Owner.GetCulture().ID);
                }
            }

            foreach (var army in defender_armies)
            {
                var defender_holyorder_regiments = army.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments)
                                              .Where(regiment => regiment.isMercenary() && regiment.Culture == null);
                foreach (var holy_regiment in defender_holyorder_regiments)
                {
                    holy_regiment.SetCulture(army.Owner.GetCulture().ID);
                }
            }
        }

        private static void ReadCultureManager()
        {
            Armies_Functions.ReadArmiesCultures(attacker_armies);
            Armies_Functions.ReadArmiesCultures(defender_armies);
        }

        private static void ReadCountiesManager()
        {

            List<(string county_key, string culture_id)> FoundCounties = new List<(string county_key, string culture_id)>();

            bool isSearchStared = false;
            string county_key = "";
            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Counties_Path()))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;

                    //County Line
                    if(Regex.IsMatch(line,@"\t\t\w+={") && !isSearchStared)
                    {
                        county_key = Regex.Match(line, @"\t\t(\w+)={").Groups[1].Value;

                        isSearchStared =  Armies_Functions.SearchCounty(county_key, attacker_armies);
                        if (!isSearchStared)
                        {
                            isSearchStared = Armies_Functions.SearchCounty(county_key, defender_armies);
                        }
                        
                    }

                    //Culture ID
                    else if(isSearchStared && line.Contains("\t\t\tculture=")) 
                    {
                        string culture_id = Regex.Match(line, @"\t\t\tculture=(\d+)").Groups[1].Value;
                        FoundCounties.Add((county_key, culture_id));                        
                    }

                    // County End Line
                    else if(isSearchStared && line == "\t\t}")
                    {
                        isSearchStared = false;
                    }


                }

                
                //Populate regiments with culture id's
                Armies_Functions.PopulateRegimentsWithCultures(FoundCounties, attacker_armies);
                Armies_Functions.PopulateRegimentsWithCultures(FoundCounties, defender_armies);
 
            }
        }
        


        private static void ReadRegiments()
        {
            bool isSearchStarted = false;
            Regiment regiment = null;

            int index = -1;
            int reg_chunk_index = 0;

            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Regiments_Path()))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;

                    // Regiment ID Line
                    if (Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        string regiment_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;

                        var searchingData = Armies_Functions.SearchRegiments(regiment_id, attacker_armies);
                        if(searchingData.searchHasStarted)
                        {
                            isSearchStarted = true;
                            regiment = searchingData.regiment;
                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchRegiments(regiment_id, defender_armies);
                            if( searchingData.searchHasStarted )
                            {
                                isSearchStarted = true;
                                regiment = searchingData.regiment;
                            }
                        }
                    }

                    // Index Counter
                    else if (line == "\t\t\t\t{" && isSearchStarted)
                    {
                        string str_index = regiment.Index;
                        if (!string.IsNullOrEmpty(str_index))
                        {
                            reg_chunk_index = Int32.Parse(str_index);
                            index++;
                        }
                        else
                        {
                            reg_chunk_index = 0;
                            index++;
                        }
                    }

                    // isMercenary 
                    else if (isSearchStarted && line.Contains("\t\t\tsource=hired"))
                    {
                        regiment.isMercenary(true);

                    }

                    // isGarrison 
                    else if (isSearchStarted && line.Contains("\t\t\tsource=garrison"))
                    {
                        regiment.IsGarrison(true);

                    }
                    // Origin 
                    else if (isSearchStarted && line.Contains("\t\t\torigin="))
                    {
                        string origin = Regex.Match(line, @"\d+").Value;
                        regiment.SetOrigin(origin);

                    }
                    // Owner 
                    else if (isSearchStarted && line.Contains("\t\t\towner="))
                    {
                        string owner = Regex.Match(line, @"\d+").Value;
                        regiment.SetOwner(owner);

                    }
                    else if(isSearchStarted && line.Contains("\t\t\towning_title="))
                    {
                        string owiningTitle = Regex.Match(line, @"\d+").Value;
                        regiment.SetOwningTitle(owiningTitle);
                    }
                    // Max
                    else if (isSearchStarted && line.Contains("\t\t\t\t\tmax="))
                    {
                        string max = Regex.Match(line, @"\d+").Value;
                        regiment.SetMax(max);
                    }

                    // Soldiers
                    else if (isSearchStarted && (line.Contains("\t\t\t\t\tcurrent=") || line.Contains("\t\t\tsize=")))
                    {
                        string current = Regex.Match(line, @"\d+").Value;
                        if (index == reg_chunk_index || (index == -1 && reg_chunk_index == 0))
                        {
                            regiment.SetSoldiers(current);
                        }

                        if(line.Contains("\t\t\tsize="))
                        {
                            regiment.SetMax(current);
                        }
                    }

                    //Regiment End Line
                    else if (isSearchStarted && line == "\t\t}")
                    {
                        isSearchStarted = false;
                        index = -1;
                        reg_chunk_index = 0;

                        EditOgRegiment(regiment, attacker_armies, defender_armies);

                        regiment = null;
                       
                    }
                }
            }
            RemoveGarrisonRegiments(attacker_armies, defender_armies);
        }

        static void RemoveGarrisonRegiments(List<Army> attacker_armies, List<Army> defender_armies)
        {
            for (int i = 0; i < attacker_armies.Count; i++)
            {
                attacker_armies[i].RemoveGarrisonRegiments();
            }
            for (int i = 0; i < defender_armies.Count; i++)
            {
                defender_armies[i].RemoveGarrisonRegiments();
            }
        }
        static void EditOgRegiment(Regiment editedRegiment ,List<Army> attacker_armies, List<Army> defender_armies)
        {
            foreach(Army army in attacker_armies)
            {
                foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    foreach(Regiment regiment in armyRegiment.Regiments)
                    {
                        if(editedRegiment.ID == regiment.ID)
                        {
                            regiment.SetOrigin(editedRegiment.Origin);
                            regiment.SetMax(editedRegiment.Max);
                            regiment.SetSoldiers(editedRegiment.CurrentNum);
                            regiment.SetOwner(editedRegiment.Owner);
                            regiment.isMercenary(editedRegiment.isMercenary());
                            regiment.IsGarrison(editedRegiment.IsGarrison());
                            return;
                        }
                        
                    }
                }
            }

            foreach (Army army in defender_armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        if (editedRegiment.ID == regiment.ID)
                        {
                            regiment.SetOrigin(editedRegiment.Origin);
                            regiment.SetMax(editedRegiment.Max);
                            regiment.SetSoldiers(editedRegiment.CurrentNum);
                            regiment.SetOwner(editedRegiment.Owner);
                            regiment.isMercenary(editedRegiment.isMercenary());
                            regiment.IsGarrison(editedRegiment.IsGarrison());
                            return;
                        }

                    }
                }
            }

        }


        private static void ReadArmiesUnits()
        {
            bool isSearchStarted = false;
            Army army = null;

            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.Units_Path()))
            {
                while(!SR.EndOfStream)
                {
                    string line  = SR.ReadLine();
                    if (line == null) break;

                    if (Regex.IsMatch(line, @"\t\d+={") && !isSearchStarted)
                    {
                        string id = Regex.Match(line, @"\t(\d+)={").Groups[1].Value;
                        var searchingData = Armies_Functions.SearchUnit(id, attacker_armies);
                        if(searchingData.searchHasStarted)
                        {
                            isSearchStarted = true;
                            army = searchingData.army;
                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchUnit(id, defender_armies);
                            if(searchingData.searchHasStarted)
                            {
                                isSearchStarted = true;
                                army= searchingData.army;
                            }
                        }
                    }
                    else if(isSearchStarted && line.Contains("\t\towner="))
                    {
                        string id = Regex.Match(line, @"\d+").Value;
                        army.SetOwner(id);

                    }
                    else if (isSearchStarted && line == "\t}")
                    {
                        isSearchStarted = false;
                    }

                }
            }
        }

        
        private static void ReadArmyRegiments()
        {
            List<Regiment> found_regiments = new List<Regiment>();

            bool isSearchStarted = false;
            ArmyRegiment armyRegiment = null;

            string regiment_id = "";
            string index = "";

            bool isNameSet = false;
            bool isReadingChunks = false;

            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.ArmyRegiments_Path()))
            {
                while (true)
                {
                    string line = SR.ReadLine();

                    if (line == null) break;

                    // Army Regiment ID Line
                    if (Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        string army_regiment_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        var searchingData = Armies_Functions.SearchArmyRegiments(army_regiment_id, attacker_armies);
                        if(searchingData.searchHasStarted)
                        {
                            isSearchStarted = true;
                            armyRegiment = searchingData.regiment;
                        }
                        else
                        {
                            searchingData = Armies_Functions.SearchArmyRegiments(army_regiment_id, defender_armies);
                            if(searchingData.searchHasStarted)
                            {
                                isSearchStarted = true;
                                armyRegiment = searchingData.regiment;
                            }
                        }
                    }

                    //Regiment ID
                    if(isSearchStarted && line.Contains("\t\t\t\t\tregiment="))
                    {
                        if(isNameSet == false)
                        {
                            armyRegiment.SetType(RegimentType.Levy);
                        }

                        regiment_id = Regex.Match(line, @"(\d+)").Groups[1].Value;                    

                    }

                    else if(isSearchStarted && line.Contains("\t\t\tchunks={"))
                    {
                        isReadingChunks = true;
                    }

                    //Regiment Index
                    else if (isSearchStarted && line.Contains("\t\t\t\t\tindex="))
                    {
                        index = Regex.Match(line, @"(\d+)").Groups[1].Value;
                    }

                    //Add Found Regiment
                    else if (isSearchStarted && line == "\t\t\t\t}" && isReadingChunks)
                    {
                        Regiment regiment = new Regiment(regiment_id, index);
                        found_regiments.Add(regiment);
                    }
                    else if (isSearchStarted && line == " }" && isReadingChunks)
                    {
                        isReadingChunks = false;
                    }

                    //Current Number
                    else if(isSearchStarted && line.Contains("\t\t\t\tcurrent="))
                    {
                        string currentNum = Regex.Match(line, @"\d+").Value;
                        armyRegiment.SetCurrentNum(currentNum);
                    }

                    //Max
                    else if (isSearchStarted && line.Contains("\t\t\t\tmax="))
                    {
                        string max = Regex.Match(line, @"\d+").Value;
                        armyRegiment.SetMax(max);
                    }

                    //Men At Arms
                    else if (isSearchStarted && line.Contains("\t\t\ttype="))
                    {
                        string type = Regex.Match(line, "type=(.+)").Groups[1].Value;
                        armyRegiment.SetType(RegimentType.MenAtArms, type);
                        isNameSet = true;
                    }

                    //Knight
                    else if (isSearchStarted && line.Contains("\t\t\tknight="))
                    {
                        string character_id = Regex.Match(line, @"knight=(\d+)").Groups[1].Value;
                        armyRegiment.SetType(RegimentType.Knight, character_id);
                        isNameSet = true;
                    }

                    
                    //Levies
                    else if (isSearchStarted && line == "\t\t\t\tlevies={")
                    {
                        armyRegiment.SetType(RegimentType.Levy);
                        isNameSet = true;
                    }

                    // Army Regiment End Line
                    else if (line == "\t\t}" && isSearchStarted)
                    {
                        //Debug purposes, remove later...
                        if(found_regiments != null)
                        {
                            armyRegiment.SetRegiments(found_regiments);
                        }

                        found_regiments = new List<Regiment>();
                        regiment_id = "";
                        index = "";
                        isSearchStarted = false;
                        isNameSet= false;
                        isReadingChunks = false;
                    }

                }
            }

            ClearNullArmyRegiments();
        }


        private static void ReadArmiesData()
        {
            bool isSearchStarted = false;
            bool isDefender = false, isAttacker = false;
            int index = 0;
            using (StreamReader SR = new StreamReader(Writter.DataFilesPaths.Armies_Path()))
            {
                while(true)
                {
                    string line = SR.ReadLine();
                    if (line == null) break;

                    // Army ID Line
                    if(Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStarted)
                    {
                        // Check if it's a battle army

                        string army_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;
                        for (int i = 0; i < attacker_armies.Count; i++)
                        {
                            if (attacker_armies[i].ID == army_id)
                            {
                                index = i;
                                isAttacker = true;
                                isDefender = false;
                                isSearchStarted = true;
                                break;
                            }

                        }
                        if(!isSearchStarted)
                        {
                            for (int i = 0; i < defender_armies.Count; i++)
                            {
                                if (defender_armies[i].ID == army_id)
                                {
                                    index = i;
                                    isDefender = true;
                                    isAttacker = false;
                                    isSearchStarted = true;
                                    break;
                                }
                            }
                        }

                    }

                    // Regiments ID's Line
                    if (isSearchStarted && line.Contains("\t\t\tregiments={"))
                    {
                        MatchCollection regiments_ids = Regex.Matches(line, @"(\d+) ");
                        List<ArmyRegiment> army_regiments = new List<ArmyRegiment>();
                        foreach(Match match in regiments_ids)
                        {
                            string id_ = match.Groups[1].Value;
                            ArmyRegiment army_regiment = new ArmyRegiment(id_);
                            army_regiments.Add(army_regiment);
                        }

                        if(isAttacker)
                        {
                            attacker_armies[index].SetArmyRegiments(army_regiments);
                        }
                        else if(isDefender)
                        {
                            defender_armies[index].SetArmyRegiments(army_regiments);
                        }

                    }
                    else if(isSearchStarted && line.Contains("\t\t\tcommander="))
                    {
                        string id = Regex.Match(line, @"commander=(\d+)").Groups[1].Value;
                        if (isAttacker)
                        {
                            attacker_armies[index].CommanderID = id;
                        }
                        else if (isDefender)
                        {
                            defender_armies[index].CommanderID = id;
                        }
                    }
                    else if (isSearchStarted && line.Contains("\t\t\tunit="))
                    {
                        string armyUnitId = Regex.Match(line, @"\d+").Value;
                        if(isAttacker)
                        {
                            attacker_armies[index].ArmyUnitID = armyUnitId;
                        }
                        else if(isDefender)
                        {
                            defender_armies[index].ArmyUnitID = armyUnitId;
                        }
                    }



                    // Army End Line
                    if (isSearchStarted && line == "\t\t}")
                    {
                        index = 0;
                        isAttacker = false;
                        isDefender = false;
                        isSearchStarted = false;
                    }

                }
            }
        }

        private static void ReadCombatArmies(string g)
        {
            bool isAttacker = false, isDefender = false;

            using (StringReader SR = new StringReader(g))//Player_Combat
            {
                while (true)
                {
                    string line = SR.ReadLine();
                    if (line == null) break;

                    if (line == "\t\t\tattacker={")
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line == "\t\t\tdefender={")
                    {
                        isAttacker = false;
                        isDefender = true;
                    }
                    else if (line == "\t\t\t}")
                    {
                        isDefender = false;
                        isAttacker = false;
                    }

                    if (isAttacker && line.Contains("\t\t\t\tarmies={"))
                    {
                        MatchCollection found_armies = Regex.Matches(line, @"(\d+) ");
                        attacker_armies = new List<Army>();

                        for(int i = 0; i < found_armies.Count; i++)
                        {
                            //Create new Army with combat sides on the constructor
                            //Army army
                            string id = found_armies[i].Groups[1].Value;
                            string combat_side = "attacker";

                            // main army
                            if(i == 0) //<-------------------------------------------------------------------[FIX THIS] !!!
                            {
                                Army army = new Army(id, combat_side, true);
                                attacker_armies.Add(army);
                            }
                            // ally army
                            else
                            {
                               Army army = new Army(id, combat_side, false);
                               attacker_armies.Add(army);
                            }
                        }
  
                    }
                    else if (isDefender && line.Contains("\t\t\t\tarmies={"))
                    {
                        MatchCollection found_armies = Regex.Matches(line, @"(\d+) ");
                        defender_armies = new List<Army>();

                        for (int i = 0; i < found_armies.Count; i++)
                        {
                            //Create new Army with combat sides on the constructor
                            //Army army
                            string id = found_armies[i].Groups[1].Value;
                            string combat_side = "defender";

                            // main army
                            if (i == 0)//<-------------------------------------------------------------------[FIX THIS] !!!
                            {
                                Army army = new Army(id, combat_side, true);
                                defender_armies.Add(army);
                            }
                            // ally army
                            else
                            {
                                Army army = new Army(id, combat_side, false);
                                defender_armies.Add(army);
                            }
                        }
                    }

                }
            }
        }

        static void ReadCombatSoldiersNum(string combat_string)
        {
            bool isAttacker = false, isDefender = false;
            string searchingArmyRegiment = null;
            using (StringReader SR = new StringReader(combat_string))//Player_Combat
            {
                while (true)
                {
                    string line = SR.ReadLine();
                    if (line == null) break;

                    if (line == "\t\t\tattacker={")
                    {
                        isAttacker = true;
                        isDefender = false;
                    }
                    else if (line == "\t\t\tdefender={")
                    {
                        isAttacker = false;
                        isDefender = true;
                    }
                    else if (line == "\t\t\t}")
                    {
                        isDefender = false;
                        isAttacker = false;
                    }

                    else if (isAttacker && line.Contains("\t\t\t\t\t\tregiment="))
                    {
                        searchingArmyRegiment = Regex.Match(line, @"\d+").Value;
                    }
                    else if (isDefender && line.Contains("\t\t\t\t\t\tregiment="))
                    {
                        searchingArmyRegiment = Regex.Match(line, @"\d+").Value;
                    }

                    else if(isAttacker && line.Contains("\t\t\t\t\t\tstarting="))
                    {
                        string startingNum = Regex.Match(line,@"\d+").Value;

                        foreach(var army in attacker_armies)
                        {
                            army.ArmyRegiments.FirstOrDefault(x => x.ID == searchingArmyRegiment)?.SetStartingNum(startingNum);
                        }

                    }
                    else if(isDefender && line.Contains("\t\t\t\t\t\tstarting="))
                    {
                        string startingNum = Regex.Match(line, @"\d+").Value;
                        foreach (var army in defender_armies)
                        {
                            army.ArmyRegiments.FirstOrDefault(x => x.ID == searchingArmyRegiment)?.SetStartingNum(startingNum);
                        }
                    }

                    else if((isAttacker || isDefender) && line == "\t\t\t}")
                    {
                        isAttacker = false;
                        isDefender = false;
                        searchingArmyRegiment = null;
                    }

                    //end line
                    else if(line == "\t\t}")
                    {
                        break;
                    }
 

                }
            }
        }


    }
}
