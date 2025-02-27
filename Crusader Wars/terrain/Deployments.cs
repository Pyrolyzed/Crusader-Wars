﻿using Crusader_Wars.client;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Crusader_Wars.terrain
{
    public static class Deployments
    {

        //Radius
        static string ROTATION_0º = "0.00";
        static string ROTATION_90º = "1.57";
        static string ROTATION_180º = "3.14";
        static string ROTATION_270º = "4.71";
        static string ROTATION_360º = "6.28";


        struct Directions
        {
            static string SetSouth(DeploymentArea SOUTH_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{SOUTH_DEPLOYMENT_AREA.X}\" y =\"{SOUTH_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{SOUTH_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{SOUTH_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_0º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetWest(DeploymentArea WEST_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{WEST_DEPLOYMENT_AREA.X}\" y =\"{WEST_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{WEST_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{WEST_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_180º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetEast(DeploymentArea EAST_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{EAST_DEPLOYMENT_AREA.X}\" y =\"{EAST_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{EAST_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{EAST_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_360º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }
            static string SetNorth(DeploymentArea NORTH_DEPLOYMENT_AREA)
            {
                string PR_Deployment = "<deployment_area>\n" +
                          $"<centre x =\"{NORTH_DEPLOYMENT_AREA.X}\" y =\"{NORTH_DEPLOYMENT_AREA.Y}\"/>\n" +
                          $"<width metres =\"{NORTH_DEPLOYMENT_AREA.Width}\"/>\n" +
                          $"<height metres =\"{NORTH_DEPLOYMENT_AREA.Height}\"/>\n" +
                          $"<orientation radians =\"{ROTATION_180º}\"/>\n" +
                          "</deployment_area>\n\n";
                return PR_Deployment;
            }

            public static string SetOppositeDirection(string direction, int total_soldiers)
            {
                DeploymentArea DEPLOYMENT_AREA;
                switch (direction)
                {
                    case "N":
                        DEPLOYMENT_AREA = new DeploymentArea("S", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetSouth(DEPLOYMENT_AREA);
                    case "S":
                        DEPLOYMENT_AREA = new DeploymentArea("N", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetNorth(DEPLOYMENT_AREA);
                    case "E":
                        DEPLOYMENT_AREA = new DeploymentArea("W", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetWest(DEPLOYMENT_AREA);
                    case "W":
                        DEPLOYMENT_AREA = new DeploymentArea("E", ModOptions.DeploymentsZones(), total_soldiers);
                        return Directions.SetEast(DEPLOYMENT_AREA);
                }

                return null;
            }

            public static string SetDirection(string direction, int total_soldiers)
            {
                DeploymentArea DEPLOYMENT_AREA = new DeploymentArea(direction, ModOptions.DeploymentsZones(), total_soldiers);
                switch (direction)
                {
                    case "N":
                        return Directions.SetNorth(DEPLOYMENT_AREA);
                    case "S":
                        return Directions.SetSouth(DEPLOYMENT_AREA);
                    case "E":
                        return Directions.SetEast(DEPLOYMENT_AREA);
                    case "W":
                        return Directions.SetWest(DEPLOYMENT_AREA);
                }

                return null;
            }

            public static string GetOppositeDirection(string direction)
            {
                switch (direction)
                {
                    case "N":
                        return "S";
                    case "S":
                        return "N";
                    case "E":
                        return "W";
                    case "W":
                        return "E";
                }

                return "";
            }
        }
 
        static string attacker_direction = "", defender_direction = "";
        static string attacker_deployment="", defender_deployment = "";
        public static void beta_SetSidesDirections(int total_soldiers, (string x, string y, string[] attacker_dir, string[] defender_dir) battle_map, bool shouldRotateDeployment)
        {
            Random random = new Random();
            //All directions battle maps
            if (battle_map.attacker_dir[0] == "All")
            {
                string[] coords = { "N", "S", "E", "W" };
                int index = random.Next(0, 4);
                attacker_direction = coords[index];
                defender_direction = Directions.GetOppositeDirection(attacker_direction);
                attacker_deployment =  Directions.SetDirection(attacker_direction, total_soldiers);
                defender_deployment = Directions.SetOppositeDirection(attacker_direction, total_soldiers);


            }
            //Defined directions battle maps
            else
            {
                if(shouldRotateDeployment)
                {
                    int defender_index = random.Next(0, 2);
                    defender_direction = battle_map.attacker_dir[defender_index];
                    attacker_direction = Directions.GetOppositeDirection(defender_direction);
                    defender_deployment = Directions.SetDirection(defender_direction, total_soldiers);
                    attacker_deployment = Directions.SetOppositeDirection(defender_direction, total_soldiers);
                }
                else
                {
                    int defender_index = random.Next(0, 2);
                    defender_direction = battle_map.defender_dir[defender_index];
                    attacker_direction = Directions.GetOppositeDirection(defender_direction);
                    defender_deployment = Directions.SetDirection(defender_direction, total_soldiers);
                    attacker_deployment = Directions.SetOppositeDirection(defender_direction, total_soldiers);
                }
            }

        }

        public static string beta_GetDeployment(string combat_side)
        {

            switch(combat_side)
            {
                case "attacker":
                    return attacker_deployment;
                case "defender":
                    return defender_deployment;
            }

            return attacker_deployment;
        }
        public static string beta_GeDirection(string combat_side)
        {
            switch (combat_side)
            {
                case "attacker":
                    return attacker_direction;
                case "defender":
                    return defender_direction;
            }

            return attacker_direction;
        }

    }

    class DeploymentArea
    {
        //CENTER POSITIONS
        public string X { get; private set; }
        public string Y { get; private set; }

        //AREA DIAMETER
        public string Width { get; private set; }
        public string Height { get; private set; }

        //MAP SIZE OPTION
        string MapSize { get; set; }
        public DeploymentArea(string direction, string option_map_size, int total_soldiers)
        {

            if (option_map_size == "Dynamic")
            {
                if (total_soldiers <= 5000)
                {
                    MapSize = "Medium";
                }
                else if (total_soldiers > 5000 && total_soldiers < 20000)
                {
                    MapSize = "Big";
                }
                else if (total_soldiers >= 20000)
                {
                    MapSize = "Huge";
                }
            }
            else
            {
                MapSize = option_map_size;
            }

            if (direction == "N")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = "0.00";
                        Y = "300.00";
                        HorizontalSize();
                        break;
                    case "Big":
                        X = "0.00";
                        Y = "450.00";
                        HorizontalSize();
                        break;
                    case "Huge":
                        X = "0.00";
                        Y = "700.00";
                        HorizontalSize();
                        break;
                }
            }
            else if (direction == "S")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = "0.00";
                        Y = "-300.00";
                        HorizontalSize();
                        break;
                    case "Big":
                        X = "0.00";
                        Y = "-450.00";
                        HorizontalSize();
                        break;
                    case "Huge":
                        X = "0.00";
                        Y = "-700.00";
                        HorizontalSize();
                        break;
                }
            }
            else if (direction == "W")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = "-300.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                    case "Big":
                        X = "-450.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                    case "Huge":
                        X = "-700.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                }
            }
            else if (direction == "E")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = "300.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                    case "Big":
                        X = "450.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                    case "Huge":
                        X = "700.00";
                        Y = "0.00";
                        VerticalSize();
                        break;
                }
            }
        }



        private void HorizontalSize()
        {
            switch (MapSize)
            {
                case "Medium":
                    Width = "800";
                    Height = "200";
                    break;
                case "Big":
                    Width = "1300";
                    Height = "400";
                    break;
                case "Huge":
                    Width = "1800";
                    Height = "600";
                    break;
            }
        }

        private void VerticalSize()
        {
            switch (MapSize)
            {
                case "Medium":
                    Width = "200";
                    Height = "800";
                    break;
                case "Big":
                    Width = "400";
                    Height = "1300";
                    break;
                case "Huge":
                    Width = "600";
                    Height = "1800";
                    break;
            }

        }
    }

    class UnitsDeploymentsPosition
    {
        //UNITS DEPLOYMENT AREA DEFAULT POSITION
        public int X {  get; private set; }
        public int Y { get; private set; }

        //DEPLOYMENT AREA DIRECTION
        public string Direction { get; private set; }

        //MAP SIZE USER OPTION
        private string MapSize { get; set; }

        /// <summary>
        /// Dynamic constructor for units positioning
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="option_map_size"></param>
        /// <param name="total_soldiers"></param>
        public UnitsDeploymentsPosition(string direction, string option_map_size, int total_soldiers) 
        {
            Direction = direction;
            MapSize = option_map_size;

            BattleMap(option_map_size, total_soldiers);
            UnitsPositionament();
        }

        /// <summary>
        /// Default values for unit positioning
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>

        public UnitsDeploymentsPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void AddUnitXSpacing(string direction)
        {
            int xSpacing = 15; //old 30
            if (direction is "N" || direction is "S")
            {
                X = X - xSpacing;
                Y = Y;
            }
            else if (direction is "E")
            {
                xSpacing = 10; //old 15
                X = X + xSpacing;
                Y = Y;
            }
            else if (direction is "W")
            {
                xSpacing = 10; //old 15
                X = X - xSpacing;
                Y = Y;
            }
        }
        public void AddUnitYSpacing(string direction)
        {
            int ySpacing = 10; //old 15
            if (direction is "N")
            {
                X = X;
                Y = Y + ySpacing;
            }
            else if (direction is "S")
            {
                X = X;
                Y = Y - ySpacing;
            }
            else if (direction is "E" || direction is "W")
            {
                ySpacing = 15; //old 30
                X = X;
                Y = Y + ySpacing;
            }
        }


        private void BattleMap(string option_map_size, int total_soldiers)
        {
            if(option_map_size == "Dynamic")
            {
                if (total_soldiers <= 5000)
                {
                    MapSize = "Medium";
                }
                else if (total_soldiers > 5000 && total_soldiers < 20000)
                {
                    MapSize = "Big";
                }
                else if (total_soldiers >= 20000)
                {
                    MapSize = "Huge";
                }
            }
            else
            {
                MapSize = option_map_size;
            }
        }

        public void InverseDirection()
        {
            if (Direction == "N")
            {
                Direction = "S";
            }
            else if (Direction == "S")
            {
                Direction= "N";
            }
            else if (Direction == "E")
            {
                Direction = "W";
            }
            else if (Direction == "W")
            {
                Direction = "E";
            }
        }

        private void UnitsPositionament()
        {
            if (Direction == "N")
            {
                switch(MapSize)
                {
                    case "Medium":
                        X = 0;
                        Y = 200;
                        break;
                    case "Big":
                        X = 0;
                        Y = 350;
                        break;
                    case "Huge":
                        X = 0;
                        Y = 600;
                        break;
                }
            }
            else if (Direction == "S")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = 0;
                        Y = -200;
                        break;
                    case "Big":
                        X = 0;
                        Y = -350;
                        break;
                    case "Huge":
                        X = 0;
                        Y = -600;
                        break;
                }
            }
            else if (Direction == "E")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = 200;
                        Y = 0;
                        break;
                    case "Big":
                        X = 350;
                        Y = 0;
                        break;
                    case "Huge":
                        X = 600;
                        Y = 0;
                        break;
                }
            }
            else if (Direction == "W")
            {
                switch (MapSize)
                {
                    case "Medium":
                        X = -200;
                        Y = 0;
                        break;
                    case "Big":
                        X = -350;
                        Y = 0;
                        break;
                    case "Huge":
                        X = -600;
                        Y = 0;
                        break;
                }
            }
        }
    }


}

