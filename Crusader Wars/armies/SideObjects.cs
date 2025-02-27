﻿using Crusader_Wars.armies;
using Crusader_Wars.armies.commander_traits;
using Crusader_Wars.data.save_file;
using Crusader_Wars.twbattle;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media.Media3D;

namespace Crusader_Wars
{
    public class Army
    {
        public string ID { get; set; }
        public Owner Owner { get; private set; }
        public string ArmyUnitID { get; set; }

        public List<Army> MergedArmies { get; private set; }
        public List<ArmyRegiment> ArmyRegiments { get; private set; }
        public List<Unit> Units { get; private set; }
        public KnightSystem Knights { get; private set; }
        public CommanderSystem Commander { get; private set; }
        public DefensiveSystem Defences { get; private set; }

        public string CommanderID { get; set; }
        public bool isMainArmy { get; private set; }
        bool IsPlayerArmy { get; set; }
        bool IsEnemyArmy { get; set; }

        public string RealmName { get; set; }
        public string CombatSide { get; set; }
        public UnitsResults UnitsResults { get; set; }
        public List<UnitCasualitiesReport> CasualitiesReports { get; private set; }


        public Army(string id, string combat_side, bool is_main)
        {
            ID = id;
            CombatSide = combat_side;
            isMainArmy = is_main;
        }

        //Getters
        public bool IsEnemy() { return IsEnemyArmy; }
        public bool IsPlayer() { return IsPlayerArmy; }


        //Setters
        public void AddMergedArmy(Army army) {
            if (MergedArmies == null) { MergedArmies = new List<Army>(); }
            MergedArmies.Add(army); 
        }
        public void IsPlayer(bool u) { IsPlayerArmy = u; }
        public void IsEnemy(bool u) { IsEnemyArmy = u; }
        public void SetUnits(List<Unit> l) { Units = l; }
        public void SetCommander(CommanderSystem l) { Commander = l; }
        public void SetDefences(DefensiveSystem l) { Defences = l; }

        public void SetOwner(string id) {

            if (id == CK3LogData.LeftSide.GetMainParticipant().id)
                Owner = new Owner(id, new Culture(CK3LogData.LeftSide.GetMainParticipant().culture_id));
            else if (id == CK3LogData.RightSide.GetMainParticipant().id)
                Owner = new Owner(id, new Culture(CK3LogData.RightSide.GetMainParticipant().culture_id));
            else
                Owner = new Owner(id);
        }
        public void SetArmyRegiments(List<ArmyRegiment> list) { ArmyRegiments = list; }
        public void SetKnights(KnightSystem knights){ Knights = knights; }
        public void SetCasualitiesReport(List<UnitCasualitiesReport> reports) { CasualitiesReports = reports; } 
        public void ClearNullArmyRegiments()
        {
            for (int i = 0; i < ArmyRegiments.Count; i++)
            {
                if (ArmyRegiments[i].Regiments is null)
                {
                    ArmyRegiments.Remove(ArmyRegiments[i]);
                }
            }
        }

        public void RemoveGarrisonRegiments()
        {
            this.ArmyRegiments.SelectMany(armyRegiment => armyRegiment.Regiments).ToList().RemoveAll(x => x.IsGarrison());
            this.ArmyRegiments.RemoveAll(x => (x.Regiments == null || x.Regiments.Count == 0) && x.Type == RegimentType.Levy);
        }

        public int GetTotalSoldiers()
        {
            return Units.Sum(x => x.GetSoldiers());
        }

        public void ScaleUnits(int ratio)
        {
            if (ratio > 0)
            {
                foreach (var unit in Units)
                {
                    if (unit.GetRegimentType() == RegimentType.Knight || unit.GetRegimentType() == RegimentType.Commander) continue;

                    double porcentage = (double)ratio / 100;
                    double num_ratio = unit.GetSoldiers() * porcentage;
                    num_ratio = Math.Round(num_ratio);
                    unit.ChangeSoldiers((int)num_ratio);
                }

                Console.WriteLine("Army scaled by " + (double)ratio + '%');
            }

        }

        public void RemoveNullUnits()
        {
            var ascending_list = Units.OrderBy(x => x.GetSoldiers()).ToList();
            if (ascending_list == null || ascending_list.Count < 1)
                return;

            var major_levy_culture = ascending_list[0];
            int total_soldiers = 0;
            for(int i = 0; i< Units.Count;i++)
            {
                var unit = Units[i];
                if(unit.GetObjCulture() == null)
                {
                    int soldiers = unit.GetSoldiers();
                    total_soldiers += soldiers;
                    Units.Remove(unit);
                }
            }

            foreach(var unit in Units)
            {
                if(major_levy_culture == unit)
                {
                    unit.ChangeSoldiers(unit.GetSoldiers()+total_soldiers);
                }
            }
        }

        public void PrintUnits()
        {
           
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"ARMY - {ID} | {CombatSide}");
            Console.WriteLine($"ARMY - {ID} | {CombatSide}");

            if(Commander != null)
            {
                sb.AppendLine($"## GENERAL | Name: {Commander.Name} | Soldiers: {Commander.GetUnitSoldiers()} | NobleRank: {Commander.Rank} | ArmyXP: +{Commander.GetUnitsExperience()} | Culture: {Commander.GetCultureName()} | Heritage: {Commander.GetHeritageName()}");
                Console.WriteLine($"## GENERAL | Name: {Commander.Name} | Soldiers: {Commander.GetUnitSoldiers()} | NobleRank: {Commander.Rank} | ArmyXP: +{Commander.GetUnitsExperience()} | Culture: {Commander.GetCultureName()} | Heritage: {Commander.GetHeritageName()}");
            }
            if (Knights.GetKnightsList() != null)
            {
                foreach (var knight in Knights.GetKnightsList())
                {
                    if(knight.IsAccolade())
                    {
                        sb.AppendLine($"## ACCOLADE | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()}");
                        Console.WriteLine($"## ACCOLADE | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()}");
                    }
                    else
                    {
                        sb.AppendLine($"## KNIGHT | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()}");
                        Console.WriteLine($"## KNIGHT | Name: {knight.GetName()} | Soldiers: {knight.GetSoldiers()} | Culture: {knight.GetCultureName()} | Heritage: {knight.GetHeritageName()}");
                    }
                }
            }
            foreach (var unit in Units)
            {
                if (unit.IsMerc())
                {
                    sb.AppendLine($"## Hired {unit.GetRegimentType()} | Name: {unit.GetName()} |Soldiers: {unit.GetSoldiers()} | Culture: {unit.GetCulture()} | Heritage: {unit.GetHeritage()} | Unit Key: {unit.GetAttilaUnitKey()}");
                    Console.WriteLine($"## Hired {unit.GetRegimentType()} | Name: {unit.GetName()} |Soldiers: {unit.GetSoldiers()} | Culture: {unit.GetCulture()} | Heritage: {unit.GetHeritage()} | Unit Key: {unit.GetAttilaUnitKey()}");
                }
                else
                {
                    sb.AppendLine($"## {unit.GetRegimentType()} | Name: {unit.GetName()} |Soldiers: {unit.GetSoldiers()} | Culture: {unit.GetCulture()} | Heritage: {unit.GetHeritage()} | Unit Key: {unit.GetAttilaUnitKey()}");
                    Console.WriteLine($"## {unit.GetRegimentType()} | Name: {unit.GetName()} |Soldiers: {unit.GetSoldiers()} | Culture: {unit.GetCulture()} | Heritage: {unit.GetHeritage()} | Unit Key: {unit.GetAttilaUnitKey()}");
                }
            }
            Console.WriteLine();
            sb.AppendLine();

            File.AppendAllText(@".\data\battle.log", sb.ToString());
        }


    }

    enum CharacterType
    {
        MainCommander,
        Commander,
        Knight,
    }
    public class Character
    {
        // MAIN DATA
        string ID { get; set; }
        string Name { get; set; }
        int Prowess { get; set; }
        int Martial { get; set; }
        int FeudalRank {  get; set; }

        // SECUNDARY DATA
        Culture CultureObj { get; set; }
        List<(int Index, string Key)> Traits { get; set; }
        BaseSkills BaseSkills { get; set; }

        // IDENTIFIER BOOLS
        CharacterType CharacterType { get; set; }
        bool isAccolade { get; set; }
        Accolade Accolade { get; set; }

        //AFTER BATTLE DATA
        bool hasFallen { get; set; }
        int Kills { get; set; }
    }
}
