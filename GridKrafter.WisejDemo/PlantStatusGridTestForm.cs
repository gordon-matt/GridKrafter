using System;
using System.Collections.Generic;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try PlantStatusGrid on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class PlantStatusGridTestForm : Form
    {
        public PlantStatusGridTestForm()
        {
            InitializeComponent();

            this.plantStatusGrid1.Plants = SamplePlants();
            this.plantStatusGrid1.Selected = "IA-05";
            this.plantStatusGrid1.PlantSelected += this.plantStatusGrid1_PlantSelected;
        }

        // What the component fires is shown in labelEvents.
        private void plantStatusGrid1_PlantSelected(object sender, PlantStatusGridPlantSelectedEventArgs e)
        {
            this.labelEvents.Text = "plantSelected: " + e.PlantId;
        }

        private static List<PlantStatusGridPlant> SamplePlants()
        {
            var plants = new List<PlantStatusGridPlant>();
            Action<string, string, string, bool, PlantStatusGridStatus, double, double, double> add =
                (id, name, region, owned, status, capacity, rate, kgal) => plants.Add(new PlantStatusGridPlant
                {
                    Id = id, Name = name, Region = region, Owned = owned, Status = status,
                    CapacityMmgy = capacity, RunRatePct = rate, ProductionKgal = kgal
                });

            const PlantStatusGridStatus R = PlantStatusGridStatus.Running;
            add("IA-01", "Shenandoah", "Iowa", true, R, 65, 98, 175);
            add("IA-02", "Mason City", "Iowa", true, R, 120, 101, 332);
            add("IA-03", "Galva", "Iowa", false, R, 60, 96, 158);
            add("IA-04", "Fairbank", "Iowa", true, R, 120, 99, 326);
            add("IA-05", "Jewell", "Iowa", true, R, 135, 102, 377);
            add("IA-06", "Nevada", "Iowa", true, R, 70, 97, 186);
            add("IA-07", "Menlo", "Iowa", true, PlantStatusGridStatus.Reduced, 110, 82, 247);
            add("IA-08", "Steamboat Rock", "Iowa", true, PlantStatusGridStatus.Down, 60, 0, 0);
            add("IA-09", "Lakota", "Iowa", true, R, 115, 100, 315);
            add("IA-10", "Arthur", "Iowa", false, R, 110, 94, 283);
            add("IA-11", "Superior", "Iowa", true, R, 65, 99, 176);
            add("IA-12", "Council Bluffs", "Iowa", true, R, 130, 103, 367);
            add("NE-01", "Madison", "Nebraska", true, R, 60, 98, 161);
            add("NE-02", "Wood River", "Nebraska", true, R, 130, 101, 360);
            add("NE-03", "Atkinson", "Nebraska", true, R, 50, 95, 130);
            add("NE-04", "Ravenna", "Nebraska", true, PlantStatusGridStatus.Reduced, 110, 76, 229);
            add("NE-05", "Fairmont", "Nebraska", true, R, 115, 99, 312);
            add("NE-06", "York", "Nebraska", true, R, 140, 100, 384);
            add("NE-07", "Hastings", "Nebraska", true, PlantStatusGridStatus.Turnaround, 60, 0, 0);
            add("NE-08", "Central City", "Nebraska", true, R, 115, 97, 306);
            add("NE-09", "Aurora", "Nebraska", false, R, 120, 98, 322);
            add("MN-01", "Heron Lake", "Minnesota", true, R, 60, 97, 159);
            add("MN-02", "Benson", "Minnesota", true, R, 50, 100, 137);
            add("MN-03", "Janesville", "Minnesota", true, R, 110, 98, 295);
            add("MN-04", "Albert Lea", "Minnesota", true, R, 65, 96, 171);
            add("MN-05", "Lamberton", "Minnesota", true, PlantStatusGridStatus.Reduced, 55, 71, 107);
            add("MN-06", "Welcome", "Minnesota", true, R, 115, 99, 312);
            add("MN-07", "Granite Falls", "Minnesota", false, R, 65, 95, 169);
            add("IL-01", "Cambridge", "Illinois", true, R, 135, 100, 370);
            add("IL-02", "Annawan", "Illinois", true, R, 110, 98, 295);
            add("IL-03", "Hennepin", "Illinois", true, R, 115, 101, 318);
            add("IL-04", "Lena", "Illinois", true, PlantStatusGridStatus.Reduced, 70, 79, 152);
            add("IL-05", "Robinson", "Illinois", false, R, 130, 97, 345);
            add("IL-06", "Marseilles", "Illinois", true, R, 120, 99, 326);
            add("SD-01", "Aberdeen", "South Dakota", true, R, 60, 98, 161);
            add("SD-02", "Huron", "South Dakota", true, R, 35, 96, 92);
            add("SD-03", "Mina", "South Dakota", true, PlantStatusGridStatus.Down, 55, 0, 0);
            add("SD-04", "Wentworth", "South Dakota", true, R, 130, 100, 356);
            add("SD-05", "Hudson", "South Dakota", false, R, 65, 98, 175);
            add("IN-01", "Linden", "Indiana", true, R, 120, 99, 326);
            add("IN-02", "Mt Vernon", "Indiana", true, R, 115, 100, 315);
            add("IN-03", "Cloverdale", "Indiana", false, R, 90, 97, 239);
            return plants;
        }
    }
}
