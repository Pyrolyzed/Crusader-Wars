﻿using Crusader_Wars.armies.commander_traits;
using Crusader_Wars.data.save_file;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crusader_Wars
{
    public class CommanderSystem
    {
        private class CourtPosition
        {
            string Profession { get; set; }
            string Employee_ID { get; set; }


            public CourtPosition(string profession, string employee_ID)
            {
                Profession = profession;
                Employee_ID = employee_ID;
            }
        }



        public string ID { get; private set; }
        public string Name { get; private set; }
        public int Rank { get; private set; }
        public int Martial { get; private set; }
        public int Prowess { get; private set; }
        private Culture CultureObj { get; set; }
        public List<(int Index, string Key)> Traits_List { get; private set; }

        public BaseSkills BaseSkills { get; private set; }
        public CommanderTraits CommanderTraits { get; private set; }
        private List<CourtPosition> Employees { get; set; }

        public bool hasFallen { get; private set; }
        private bool MainCommander {  get; set; }
        private Accolade Accolade { get; set; }
        private bool IsAccoladeCommander { get; set; }

        /// <summary>
        /// New object for MAIN COMMANDERS only.
        /// </summary>
        public CommanderSystem(string name, string id, int prowess, int martial, int rank, bool mainCommander)
        {
            Name = name;
            ID = id;
            Prowess = prowess;
            Martial = martial;
            Rank = rank;
            MainCommander = mainCommander;
        }

        /// <summary>
        /// New object for NON-MAIN COMMANDERS.
        /// </summary>
        public CommanderSystem(string name, string id, int prowess, int rank, BaseSkills baseSkills, Culture culture)
        {
            Name = name;
            ID = id;
            Prowess = prowess;
            Martial = baseSkills.martial;
            Rank = rank;
            BaseSkills = baseSkills;
            CultureObj = culture;
            MainCommander = false;
        }
        public void ChangeCulture(Culture obj) {CultureObj = obj;}
        public void SetBaseSkills(BaseSkills t) { BaseSkills = t; }
        public void SetAccolade(Accolade accolade) { Accolade = accolade; IsAccoladeCommander = true; }

        public string GetCultureName() { return CultureObj.GetCultureName(); }
        public string GetHeritageName() { return CultureObj.GetHeritageName(); }
        public Culture GetCultureObj () { return CultureObj; }
        public bool IsMainCommander() { return MainCommander; }
        public Accolade GetAccolade() { return Accolade; }
        public bool IsAccolade() { return IsAccoladeCommander; }






        public void AddCourtPosition(string profession, string id)
        {
            if (Employees is null) Employees = new List<CourtPosition>();
            Employees.Add(new CourtPosition(profession, id));
        }

        public int GetUnitSoldiers()
        {
            return UnitSoldiers();
        }


        public int GetUnitsExperience()
        {
            return MartialArmyExperience();
        }

        public int GetCommanderExperience()
        {
            return (int)Math.Round(ProwessExperience() + (ProwessExperience() * MartialExperience()));
        }

        public int GetCommanderStarRating()
        {
            return StarExperience();
        }

        public void SetTraits(List<(int, string)> traits)
        {
            Traits_List = traits;

            if(MainCommander)
            {
                CommanderTraits = new CommanderTraits(Traits_List);
            }
        }

        int UnitSoldiers()
        {

            //Title rank soldiers
            int soldiers = 0;
            switch (Rank)
            {
                case 1:
                    soldiers = 10;
                    break;
                case 2:
                    soldiers = 20;
                    break;
                case 3:
                    soldiers = 30;
                    break;
                case 4:
                    soldiers = 50;
                    break;
                case 5:
                    soldiers = 70;
                    break;
                case 6:
                    soldiers = 90;
                    break;
            }

            //Prowess soldiers
            int prowess = Prowess;
            if (prowess <= 4)
            {
                soldiers += 0;

            }
            else if (prowess >= 5 && prowess <= 8)
            {
                soldiers += 5;
            }
            else if (prowess >= 9 && prowess <= 12)
            {
                soldiers += 10;
            }
            else if (prowess >= 13 && prowess <= 16)
            {
                soldiers += 15;
            }
            else if (prowess >= 17)
            {
                soldiers += 20;
            }

            //Court positions soldiers
            if (Employees != null)
            {
                int courtiers = Employees.Count * 5;
                soldiers += courtiers;
            }

            //Health soldiers debuff
            foreach(var trait in Traits_List)
            {
                if (trait.Index == WoundedTraits.Wounded()) soldiers += -5;
                if (trait.Index == WoundedTraits.Severely_Injured()) soldiers += -10;
                if (trait.Index == WoundedTraits.Brutally_Mauled()) soldiers += -15;
                if (trait.Index == WoundedTraits.Maimed()) soldiers += -10;
                if (trait.Index == WoundedTraits.One_Eyed()) soldiers += -5;
                if (trait.Index == WoundedTraits.One_Legged()) soldiers += -10;
                if (trait.Index == WoundedTraits.Disfigured()) soldiers += -5;
            }

            

            //Minimum of 1 soldier
            if (soldiers < 1) soldiers = 1;

            return soldiers;
        }


        int StarExperience()
        {
            int martial = Martial;
            int value = 0;

            if (martial <= 3)
            {
                //Terrible Martial
                value += 1;

            }
            else if (martial >= 4 && martial <= 7)
            {
                //Poor Martial
                value += 2;
            }
            else if (martial >= 8 && martial <= 11)
            {
                //Averege Martial
                value += 3;
            }
            else if (martial >= 12 && martial <= 15)
            {
                //Good Martial
                value += 4;
            }
            else if (martial >= 16)
            {
                //Excelent Martial
                value += 5;
            }

            if (value > 9) value = 9;
            return value;
        }

        double ProwessExperience()
        {
            int prowess = Prowess;
            int value = 0;
            if (prowess <= 4)
            {
                value += MartialSkill.Terrible();

            }
            else if (prowess >= 5 && prowess <= 8)
            {
                value += MartialSkill.Poor();
            }
            else if (prowess >= 9 && prowess <= 12)
            {
                value += MartialSkill.Average();
            }
            else if (prowess >= 13 && prowess <= 16)
            {
                value += MartialSkill.Good();
            }
            else if (prowess >= 17)
            {
                value += MartialSkill.Excellent();
            }

            return value;
        }

        double MartialExperience()
        {
            int martial = Martial;
            double value = 0;

            if (martial <= 3)
            {
                value += 0.0;

            }
            else if (martial >= 4 && martial <= 7)
            {
                value += 0.2;
            }
            else if (martial >= 8 && martial <= 11)
            {
                value += 0.4;
            }
            else if (martial >= 12 && martial <= 15)
            {
                value += 0.6;
            }
            else if (martial >= 16)
            {
                value += 0.8;
            }


            return value;
        }

        int MartialArmyExperience()
        {
            int martial = Martial;
            int value = 0;

            if (martial <= 3)
            {
                value += MartialSkill.Terrible();

            }
            else if (martial >= 4 && martial <= 7)
            {
                value += MartialSkill.Poor();
            }
            else if (martial >= 8 && martial <= 11)
            {
                value += MartialSkill.Average();
            }
            else if (martial >= 12 && martial <= 15)
            {
                value += MartialSkill.Good();
            }
            else if (martial >= 16)
            {
                value += MartialSkill.Excellent();
            }


            return value;

        }

        
        public void HasGeneralFallen(string path_attila_log)
        {
            using (FileStream logFile = File.Open(path_attila_log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(logFile))
            {
                string str = reader.ReadToEnd();

                if (str.Contains($"Commander{ID} from Army"))
                {
                    hasFallen = true;
                    Console.WriteLine($"Commander {ID} has fallen!");

                    reader.Close();
                    logFile.Close();
                    return;
                }

                hasFallen = false;
            }
        }
        

        public string Health(string traits_line)
        {
            //50% Chance Light Wounds
            const int WoundedChance = 50; // 50%

            //50% Chance Harsh Wounds
            const int Severely_InjuredChance = 70; // 20%
            const int Brutally_MauledChance = 90; // 20%

            //10% Chance Extreme Wounds
            const int MaimedChance = 93; // 3%
            const int One_LeggedChance = 96;// 3%
            const int One_EyedChance = 99;// 3%
            const int Disfigured = 100; //1%

            var Chance = new Random();
            var RandomNumber = Chance.Next(101);

            if (hasFallen)
            {

                // Determine which option to set based on its percentage chance
                if (RandomNumber >= 0 && RandomNumber <= WoundedChance)
                {
                    Console.WriteLine($"Commander {ID} got "+"Wounded ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Wounded().ToString()); 
                }
                else if (RandomNumber > WoundedChance && RandomNumber <= Severely_InjuredChance)
                {
                    Console.WriteLine($"Commander {ID} got " + "Severely_Injured ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Severely_Injured().ToString());
                }
                else if (RandomNumber > Severely_InjuredChance && RandomNumber <= Brutally_MauledChance)
                {
                    Console.WriteLine($"Commander {ID} got " + "Brutally Mauled ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Brutally_Mauled().ToString());
                }
                else if (RandomNumber > Brutally_MauledChance && RandomNumber <= MaimedChance)
                {
                    Console.WriteLine($"Commander {ID} got " + "Maimed ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Maimed().ToString());
                }
                else if (RandomNumber > MaimedChance && RandomNumber <= One_LeggedChance)
                {
                    Console.WriteLine($"Commander {ID} got " + "One Legged ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Legged().ToString());
                }
                else if (RandomNumber > One_LeggedChance && RandomNumber <= One_EyedChance)
                {
                    Console.WriteLine($"Commander {ID} got " + "One Eyed ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.One_Eyed().ToString());
                }
                else if (RandomNumber > One_EyedChance && RandomNumber <= Disfigured)
                {
                    Console.WriteLine($"Commander {ID} got " + "Disfigured ");
                    return CharacterWounds.VerifyTraits(traits_line, WoundedTraits.Disfigured().ToString());
                }

            }

            return traits_line;
        }
    }
}
