﻿using Crusader_Wars.client;
using Crusader_Wars.unit_mapper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Crusader_Wars.data.save_file
{
    internal static class Armies_Functions
    {
        /*##############################################
         *####               COUNTIES               #### 
         *####--------------------------------------####
         *####   Reader for the counties manager    ####
         *##############################################
         */

        public static bool SearchCounty(string county_key, List<Army> armies)
        {
            foreach(Army army in armies)
            {
                foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null)
                        continue;

                    foreach(Regiment regiment in armyRegiment.Regiments)
                    {
                        //if county key is empty, skip
                        if (string.IsNullOrEmpty(regiment.GetCountyKey()))
                        {
                            continue;
                        }
                        else
                        {
                            string regiment_county_key = regiment.GetCountyKey();
                            if (regiment_county_key == county_key)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static void PopulateRegimentsWithCultures(List<(string county_key, string culture_id)> foundCultures, List<Army> armies)
        {
            var temp_characters_cultures = new List<(string char_id, string culture_id)>();

            foreach (Army army in armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null)
                        continue;

                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        string owner_id = regiment.Owner;
                        if (!string.IsNullOrEmpty(owner_id))
                        {
                            if (temp_characters_cultures.Exists(t => t.char_id == owner_id))
                            {
                                string culture_id = temp_characters_cultures.FirstOrDefault(p => p.char_id == owner_id).culture_id;
                                regiment.SetCulture(culture_id);
                            }
                            else
                            {
                                string culture_id = GetCharacterCultureID(owner_id);
                                temp_characters_cultures.Add((owner_id, culture_id));
                                regiment.SetCulture(culture_id);
                            }

                        }
                        else if (foundCultures.Exists(culture => culture.county_key == regiment.GetCountyKey()))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.county_key == regiment.GetCountyKey());
                            regiment.SetCulture(foundCulture.culture_id);
                        }
                    }
                }
            }
        }

        static string GetCharacterCultureID(string character_id)
        {
            bool isSearchStarted = false;
            string culture_id = "";
            using (StreamReader stringReader = new StreamReader(Writter.DataFilesPaths.Living_Path()))
            {
                while (true)
                {
                    string line = stringReader.ReadLine();
                    if (line == null) break;

                    if (line == $"{character_id}={{" && !isSearchStarted)
                    {
                        isSearchStarted = true;
                    }

                    if (isSearchStarted && Regex.IsMatch(line, @"\tculture=\d+"))
                    {
                        culture_id = Regex.Match(line, @"\tculture=(\d+)").Groups[1].Value;
                        return culture_id;
                    }

                    if (isSearchStarted && line == "}")
                    {
                        return "";
                    }
                }

                return culture_id;
            }

        }

        /*##############################################
         *####               CULTURES               #### 
         *####--------------------------------------####
         *####   Reader for the culture manager     ####
         *##############################################
         */

        public static void ReadArmiesCultures(List<Army> armies)
        {
            bool isSearchStared = false;
            string culture_id = "";
            string culture_name = "";
            string heritage_name = "";
            var found_cultures = new List<(string culture_id, string culture_name, string heritage_name)>();

            using (StreamReader sr = new StreamReader(Writter.DataFilesPaths.Cultures_Path()))
            {
                while (true)
                {
                    string line = sr.ReadLine();
                    if (line == null) break;

                    //Culture Line
                    if (Regex.IsMatch(line, @"\t\t\d+={") && !isSearchStared)
                    {
                        culture_id = Regex.Match(line, @"\t\t(\d+)={").Groups[1].Value;

                        //Armies
                        foreach (Army army in armies)
                        {
                            //Owner
                            if (army.Owner.GetCulture() != null && army.Owner.GetCulture().ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }
                            //Commanders
                            else if (army.Commander != null && army.Commander.GetCultureObj().ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }
                            //Knights
                            else if (army.Knights != null && army.Knights.GetKnightsList() != null)
                            {
                                foreach (var knight in army.Knights.GetKnightsList())
                                {
                                    if (knight.GetCultureObj() != null && knight.GetCultureObj().ID == culture_id)
                                    {
                                        isSearchStared = true;
                                        break;
                                    }
                                }

                                if (isSearchStared)
                                    break;
                            }
                            //Army Regiments
                            foreach(ArmyRegiment armyRegiments in army.ArmyRegiments)
                            {
                                if (armyRegiments.Regiments == null) 
                                    continue;
                                //Regiments
                                foreach(Regiment regiment in armyRegiments.Regiments)
                                {
                                    if(regiment.Culture == null)
                                    {
                                        Console.WriteLine($"WARNING - REGIMENT {regiment.ID} HAS A NULL CULTURE");
                                        continue;
                                    }
                                    else if(string.IsNullOrEmpty(regiment.Culture.ID))
                                    {
                                        Console.WriteLine($"WARNING - REGIMENT {regiment.ID} DOESN'T HAVE AN ID");
                                        continue;
                                    }
                                    else
                                    {
                                        if (regiment.Culture.ID == culture_id)
                                        {
                                            isSearchStared = true;
                                            break;
                                        }
                                    }
                                }
                                if (isSearchStared)
                                    break;
                            }
                            if (isSearchStared)
                                break;
                        }

                        //----- old below-----
                        /*
                        //Armies
                        for (int i = 0; i < armies.Count; i++)
                        {
                            if (isSearchStared) break;

                            //Owner
                            if (armies[i].Owner.GetCulture() != null && armies[i].Owner.GetCulture().ID == culture_id)
                            {
                                isSearchStared = true;
                                break;
                            }

                            //Commanders
                            if (armies[i].Commander != null)
                            {
                                if (armies[i].Commander.GetCultureObj() == null)
                                {
                                    if (armies[i].IsPlayer())
                                    {
                                        armies[i].Commander.ChangeCulture(new Culture(CK3LogData.LeftSide.GetCommander().culture_id));
                                    }
                                    else if (armies[i].IsEnemy())
                                    {
                                        armies[i].Commander.ChangeCulture(new Culture(CK3LogData.RightSide.GetCommander().culture_id));
                                    }
                                    else
                                    {
                                        armies[i].Commander.ChangeCulture(armies[i].Owner.GetCulture());
                                    }
                                    
                                    continue;
                                }
                                if (armies[i].Commander.GetCultureObj().ID == culture_id)
                                {
                                    isSearchStared = true;
                                    break;
                                }
                            }

                            //Knights
                            if (armies[i].Knights.GetKnightsList() != null)
                            {
                                foreach (var knight in armies[i].Knights.GetKnightsList())
                                {
                                    string knight_culture_id = "";
                                    if (knight.GetCultureObj() == null)
                                    {
                                        Culture mainParticipantCulture = null;
                                        if (armies[i].IsPlayer())
                                        {
                                            string id = CK3LogData.LeftSide.GetMainParticipant().culture_id;
                                            mainParticipantCulture = new Culture(id);
                                        }
                                        else if(armies[i].IsEnemy())
                                        {
                                            string id = CK3LogData.RightSide.GetMainParticipant().culture_id;
                                            mainParticipantCulture = new Culture(id);
                                        }
                                            
                                        Culture new_culture = armies[i].Knights.GetKnightsList()?.Find(x => x.GetCultureObj() != null)?.GetCultureObj() ?? mainParticipantCulture;
                                        knight.ChangeCulture(new_culture);
                                        knight_culture_id = knight.GetCultureObj().ID;
                                        armies[i].Knights.SetMajorCulture();
                                    }
                                    else
                                    {
                                        knight_culture_id = knight.GetCultureObj().ID;
                                    }

                                    if (knight_culture_id == culture_id)
                                    {
                                        isSearchStared = true;
                                        break;
                                    }
                                }
                            }


                            //Army Regiments 
                            for (int x = 0; x < armies[i].ArmyRegiments.Count; x++)
                            {
                                if (isSearchStared) break;
                                if (armies[i].ArmyRegiments[x].Regiments is null) continue;
                                //Regiments
                                for (int y = 0; y < armies[i].ArmyRegiments[x].Regiments.Count; y++)
                                {


                                    //if culture is null, skip
                                    if (armies[i].ArmyRegiments[x].Regiments[y].Culture is null)
                                    {
                                        continue;
                                    }

                                    if (string.IsNullOrEmpty(armies[i].ArmyRegiments[x].Regiments[y].Culture.ID))
                                    {
                                        Console.WriteLine("WARNING - REGIMENT NULL CULTURE FOUND");
                                        //armies[i].ArmyRegiments[x].Regiments[y].Culture.SetHeritage(DataSearch.Player_Character.GetHeritage());
                                        //armies[i].ArmyRegiments[x].Regiments[y].Culture.SetName(DataSearch.Player_Character.GetCulture());
                                        continue;
                                    }
                                    else
                                    {
                                        string regiment_culture_id = armies[i].ArmyRegiments[x].Regiments[y].Culture.ID;

                                        if (regiment_culture_id == culture_id)
                                        {
                                            isSearchStared = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        */
                    }


                    //Culture Name
                    if (isSearchStared && line.Contains("\t\t\tname="))
                    {
                        culture_name = Regex.Match(line, @"""(.+)""").Groups[1].Value;
                        culture_name = culture_name.Replace("-", "");
                        culture_name = culture_name.Replace("_", "");
                        if (culture_name == "ringlóvale")
                            Console.WriteLine("fodasse");
                        culture_name = RemoveDiacritics(culture_name);

                    }
                    //Heritage Name
                    else if (isSearchStared && line.Contains("\t\t\theritage="))
                    {
                        heritage_name = Regex.Match(line, @"""(.+)""").Groups[1].Value;
                        heritage_name = heritage_name.Trim('-');
                    }

                    //End Line
                    if (isSearchStared && line == "\t\t}")
                    {
                        found_cultures.Add((culture_id, culture_name, heritage_name));
                        isSearchStared = false;
                        culture_id = ""; culture_name = ""; heritage_name = "";
                    }
                }
            }

            // This is only if there are still null cultures
            foreach (Army army in armies)
            {
                //  COMMANDERS
                if (army.Commander != null && army.Commander.GetCultureObj() == null)
                {
                    if (army.IsPlayer())
                    {
                        army.Commander.ChangeCulture(new Culture(CK3LogData.LeftSide.GetCommander().culture_id));
                    }
                    else if (army.IsEnemy())
                    {
                        army.Commander.ChangeCulture(new Culture(CK3LogData.RightSide.GetCommander().culture_id));
                    }
                    else
                    {
                        army.Commander.ChangeCulture(army.Owner.GetCulture());
                    }
                }

                // KNIGHTS
                if (army.Knights != null && army.Knights.GetKnightsList() != null)
                {
                    foreach (Knight knight in army.Knights.GetKnightsList())
                    {
                        if (knight.GetCultureObj() == null)
                        {
                            Culture mainParticipantCulture = null;
                            if (army.IsPlayer())
                            {
                                string id = CK3LogData.LeftSide.GetMainParticipant().culture_id;
                                mainParticipantCulture = new Culture(id);
                            }
                            else if (army.IsEnemy())
                            {
                                string id = CK3LogData.RightSide.GetMainParticipant().culture_id;
                                mainParticipantCulture = new Culture(id);
                            }

                            Culture new_culture = army.Knights.GetKnightsList()?.Find(x => x.GetCultureObj() != null)?.GetCultureObj() ?? mainParticipantCulture;
                            knight.ChangeCulture(new_culture);
                            //knight_culture_id = knight.GetCultureObj().ID;
                            army.Knights.SetMajorCulture();
                        }
                    }
                }

            }

            SetCulturesToAll(armies, found_cultures);
        }

        static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder
                .ToString()
                .Normalize(NormalizationForm.FormC);
        }

        internal static void SetCulturesToAll(List<Army> armies, List<(string culture_id, string culture_name, string heritage_name)> foundCultures)
        {
            foreach (Army army in armies)
            {

                //Owner
                if (army.Owner.GetCulture() != null && foundCultures.Exists(culture => culture.culture_id == army.Owner.GetCulture().ID))
                {
                    var foundCulture = foundCultures.Find(culture => culture.culture_id == army.Owner.GetCulture().ID);
                    army.Owner.GetCulture().SetName(foundCulture.culture_name);
                    army.Owner.GetCulture().SetHeritage(foundCulture.heritage_name);
                }

                //Commanders
                if (army.Commander != null)
                {
                    if (foundCultures.Exists(culture => culture.culture_id == army.Commander.GetCultureObj().ID))
                    {
                        var foundCulture = foundCultures.Find(culture => culture.culture_id == army.Commander.GetCultureObj().ID);
                        army.Commander.GetCultureObj().SetName(foundCulture.culture_name);
                        army.Commander.GetCultureObj().SetHeritage(foundCulture.heritage_name);
                    }
                }

                //Knights
                if (army.Knights.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        string knight_culture_id = knight.GetCultureObj().ID;
                        if (foundCultures.Exists(culture => culture.culture_id == knight_culture_id))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.culture_id == knight_culture_id);
                            knight.GetCultureObj().SetName(foundCulture.culture_name);
                            knight.GetCultureObj().SetHeritage(foundCulture.heritage_name);
                        }
                    }
                }


                //Army Regiments
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if (armyRegiment.Regiments == null) continue;
                    foreach (Regiment regiment in armyRegiment.Regiments)
                    {
                        if (regiment.Culture == null)
                        {
                            Console.WriteLine($"WARNING - NULL CULTURE IN REGIMENT {regiment.ID}");
                            continue;
                        }
                        string regimentCultureID = regiment.Culture.ID;
                        if (foundCultures.Exists(culture => culture.culture_id == regimentCultureID))
                        {
                            var foundCulture = foundCultures.Find(culture => culture.culture_id == regimentCultureID);
                            regiment.Culture.SetName(foundCulture.culture_name);
                            regiment.Culture.SetHeritage(foundCulture.heritage_name);
                        }

                    }
                }
            }
        }

        /*##############################################
         *####                  Unit                #### 
         *####--------------------------------------####
         *####         Searcher for unit file       ####
         *##############################################
         */

        public static (bool searchHasStarted, Army army) SearchUnit(string unitID, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                if(unitID == army.ArmyUnitID)
                {
                    return (true, army);
                }
            }
            return (false, null);
        }

        /*##############################################
         *####             Army Regiments           #### 
         *####--------------------------------------####
         *####    Searcher for army regiments file  ####
         *##############################################
         */

        public static (bool searchHasStarted, ArmyRegiment regiment) SearchArmyRegiments(string armyRegimentId, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                foreach (ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if(armyRegimentId == armyRegiment.ID)
                    {
                        return (true, armyRegiment);
                    }
                }
            }

            return (false, null);
        }

        /*##############################################
         *####               Regiments              #### 
         *####--------------------------------------####
         *####      Searcher for regiments file     ####
         *##############################################
         */

        public static (bool searchHasStarted, Regiment regiment) SearchRegiments(string regiment_id, List<Army> armies)
        {
            foreach(Army army in armies)
            {
                foreach(ArmyRegiment armyRegiment in army.ArmyRegiments)
                {
                    if(armyRegiment.Regiments != null)
                    {
                        foreach(Regiment regiment in armyRegiment.Regiments)
                        {
                            if(regiment.ID == regiment_id)
                            {
                                return (true, regiment);
                            }
                        }
                    }
                }
            }
            return (false, null);
        }

        /*##############################################
         *####              CHARACTERS              #### 
         *####--------------------------------------####
         *####      Reader for the living file      ####
         *##############################################
         */

        public static (bool searchStarted, Army searchingArmy, bool isCommander, bool isMainCommander, bool isKnight, Knight knight, bool isOwner) SearchCharacters(string id, List<Army> armies)
        {
            foreach (Army army in armies)
            {
                //Main Commanders
                if(army.Commander != null && id == army.Commander.ID && id == army.Owner.GetID())
                {
                    return (true, army, false, true, false, null, true);
                }
                else if (army.Commander != null && id == army.Commander.ID)
                {
                    return (true, army, false, true,false, null, false);
                }

                //Commanders
                if(id == army.CommanderID && id == army.Owner.GetID())
                {
                    return (true, army, true, false, false, null, true);
                }
                else if (id == army.CommanderID)
                {
                    return (true, army, true, false, false, null, false);
                }

                // KNIGHTS
                else if (army.Knights.GetKnightsList() != null)
                {
                    foreach (var knight in army.Knights.GetKnightsList())
                    {
                        if (id == knight.GetID() && id == army.Owner.GetID())
                        {
                            return (true, army, false, false,true, knight, true);
                        }
                        else if(id == knight.GetID())
                        {
                            return (true, army, false, false, true, knight, false);
                        }
                    }
                }
                //ARMY OWNER
                else if (id == army.Owner.GetID())
                {
                    return (true, army, false, false,false, null, true);
                }
            }

            return (false, null, false, false,false, null, false);
        }

        /*##############################################
         *####                UNITS                 #### 
         *####--------------------------------------####
         *####   Conversion of regiments to units   ####
         *##############################################
         */

        internal static List<Unit> GetAllUnits_UnitKeys(List<Unit> units)
        {

            //Set Unit Keys
            foreach (var unit in units)
            {
                if (unit.GetRegimentType() == RegimentType.Levy) continue;

                string key = UnitMappers_BETA.GetUnitKey(unit);
                if (key == "not_found")
                    unit.SetUnitKey("cha_spa_royal_cav");
                else
                    unit.SetUnitKey(key);
            }

            return units;
        }

        internal static List<Unit> GetAllUnits_AttilaFaction(List<Unit> units)
        {
            //Get Unit Mapper Faction
            foreach (var unit in units)
            {
                unit.SetAttilaFaction(UnitMappers_BETA.GetAttilaFaction(unit.GetCulture(), unit.GetHeritage()));
            }

            return units;
        }
        internal static List<Unit> GetAllUnits_Max(List<Unit> units)
        {
            //Read Unit Limit
            foreach (var unit in units)
            {
                unit.SetMax(UnitMappers_BETA.GetMax(unit));
            }

            return units;
        }

        // Function to get the top N units by soldiers count
        internal static List<Unit> GetTopUnits(List<Unit> units, int topN)
        {
            if (topN <= 0)
            {
                throw new ArgumentException("topN must be greater than 0");
            }

            // Sort units by soldiers count in descending order
            var sortedUnits = units.OrderByDescending(u => u.GetSoldiers()).ToList();

            // Take the top N units
            return sortedUnits.Take(topN).ToList();
        }

        internal static void CreateUnits(List<Army> armies)
        {
            foreach (var army in armies)
            {
                List<(Regiment regiment, RegimentType type, string maa_name)> list = new List<(Regiment regiment, RegimentType type, string maa_name)>();
                foreach (var army_regiment in army.ArmyRegiments)
                {
                    foreach (var regiment in army_regiment.Regiments)
                    {
                        list.Add((regiment, army_regiment.Type, army_regiment.MAA_Name));
                    }
                }

                List<Unit> units = new List<Unit>();
                foreach (var regiment in list)
                {
                    // if no soldiers, skip
                    if (ModOptions.FullArmies(regiment.regiment) is null) continue;

                    if (Int32.Parse(ModOptions.FullArmies(regiment.regiment)) == 0) continue;

                    Unit unit;
                    if (regiment.type == RegimentType.Levy)
                        if (regiment.regiment.isMercenary())
                            unit = new Unit("Levy", Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type, true);
                        else
                            unit = new Unit("Levy", Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type);
                    else if (regiment.type == RegimentType.MenAtArms)
                        if (regiment.regiment.isMercenary())
                            unit = new Unit(regiment.maa_name, Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type, true);
                        else
                            unit = new Unit(regiment.maa_name, Int32.Parse(ModOptions.FullArmies(regiment.regiment)), regiment.regiment.Culture, regiment.type);
                    else
                        continue;

                    if (unit != null)
                        units.Add(unit);


                }

                units = OrganizeUnitsIntoCultures(units, army.Owner);
                units = OrganizeLeviesUnits(units);
                units = GetAllUnits_AttilaFaction(units);
                units = GetAllUnits_Max(units);
                units = GetAllUnits_UnitKeys(units);
                army.SetUnits(units);
                //army.PrintUnits();
            }
        }

        static List<Unit> OrganizeUnitsIntoCultures(List<Unit> units, Owner owner)
        {
            var organizedUnits = new List<Unit>();

            // Group units by Name and Culture
            var groupedUnits = units.GroupBy(u => new { Name = u.GetName(), Culture = u.GetCulture(), Type = u.GetType(), IsMerc = u.IsMerc() });

            // Merge units with the same Name and Culture
            foreach (var group in groupedUnits)
            {
                int totalSoldiers = group.Sum(u => u.GetSoldiers());

                // Create a new Unit with the merged NumberOfSoldiers
                Unit mergedUnit = new Unit(group.Key.Name, totalSoldiers, group.ElementAt(0).GetObjCulture(), group.ElementAt(0).GetRegimentType(), group.ElementAt(0).IsMerc(), owner);
                
                organizedUnits.Add(mergedUnit);
            }

            return organizedUnits;
        }

        static List<Unit> OrganizeLeviesUnits(List<Unit> units)
        {
            var unitsBelowThreshold = units.Where(u => u.GetSoldiers() <= ModOptions.CulturalPreciseness() && u.GetName() == "Levy").ToList();
            if (unitsBelowThreshold.Count == 0) return units;

            int total = 0;
            Unit biggest = null;
            int lastRegistered = 0;
            foreach (var u in unitsBelowThreshold)
            {
                total += u.GetSoldiers();

                if (u.GetSoldiers() > lastRegistered)
                {
                    lastRegistered = u.GetSoldiers();
                    biggest = u;
                }

            }

            var unit = new Unit("Levy", total, biggest.GetObjCulture(), RegimentType.Levy);
            var unit_data = UnitsFile.RetriveCalculatedUnits(unit.GetSoldiers(), ModOptions.GetLevyMax());
            var levies_top_cultures = GetTopUnits(unitsBelowThreshold, unit_data.UnitNum);

            int null_soldiers = 0;

            var null_cultures_levies = units.Where(x => x.GetObjCulture() == null).ToList();
            if (null_cultures_levies.Count > 0)
            {
                foreach (var null_levie in null_cultures_levies)
                {
                    null_soldiers += null_levie.GetSoldiers();
                    units.Remove(null_levie);
                }
            }


            int limit = ModOptions.CulturalPreciseness();
            units.RemoveAll(x => x.GetName() == "Levy" && x.GetSoldiers() <= limit);
            for (int i = 0; i < unit_data.UnitNum; i++)
            {
                if (i == 0)
                    units.Add(new Unit("Levy", unit_data.UnitSoldiers + null_soldiers, levies_top_cultures[i].GetObjCulture(), RegimentType.Levy));
                else
                    units.Add(new Unit("Levy", unit_data.UnitSoldiers, levies_top_cultures[i].GetObjCulture(), RegimentType.Levy));
            }





            return units;
        }
    }
}
