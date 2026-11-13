using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try EthanolPriceBoard on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class EthanolPriceBoardTestForm : Form
    {
        public EthanolPriceBoardTestForm()
        {
            InitializeComponent();

            // the sketch's sample prices: spot $/gal at each hub with a 10-day history, and the benchmarks
            this.ethanolPriceBoard1.Hubs = new[]
            {
                new { Id = "chi", Hub = "Chicago Argo", Price = 1.7825, Change = 0.0125, History = new[] { 1.741, 1.748, 1.736, 1.752, 1.761, 1.757, 1.766, 1.759, 1.77, 1.7825 } },
                new { Id = "nyh", Hub = "NY Harbor", Price = 1.9275, Change = 0.0175, History = new[] { 1.882, 1.889, 1.878, 1.893, 1.904, 1.899, 1.911, 1.902, 1.91, 1.9275 } },
                new { Id = "gulf", Hub = "Gulf Coast", Price = 1.845, Change = -0.005, History = new[] { 1.861, 1.857, 1.866, 1.859, 1.852, 1.856, 1.849, 1.853, 1.85, 1.845 } },
                new { Id = "la", Hub = "Los Angeles", Price = 2.095, Change = 0.0225, History = new[] { 2.041, 2.05, 2.046, 2.058, 2.066, 2.061, 2.072, 2.069, 2.0725, 2.095 } },
                new { Id = "tpa", Hub = "Tampa", Price = 1.96, Change = 0.0, History = new[] { 1.951, 1.955, 1.949, 1.958, 1.962, 1.957, 1.963, 1.959, 1.96, 1.96 } },
                new { Id = "dal", Hub = "Dallas", Price = 1.8125, Change = -0.01, History = new[] { 1.833, 1.829, 1.835, 1.826, 1.822, 1.828, 1.819, 1.824, 1.8225, 1.8125 } }
            };
            this.ethanolPriceBoard1.Benchmarks = new[]
            {
                new { Name = "CBOT Corn", Price = 4.5325, Unit = "$/bu", Change = -0.0275, Decimals = 4 },
                new { Name = "RBOB", Price = 2.1184, Unit = "$/gal", Change = 0.0213, Decimals = 4 },
                new { Name = "CBOT Ethanol", Price = 1.795, Unit = "$/gal", Change = 0.011, Decimals = 3 }
            };
            this.ethanolPriceBoard1.AsOf = "As of 2:15 PM CT";
            this.ethanolPriceBoard1.Selected = "nyh";

            this.ethanolPriceBoard1.HubSelected += (s, e) => this.labelEvents.Text = "HubSelected: " + e.Id;
        }
    }
}
