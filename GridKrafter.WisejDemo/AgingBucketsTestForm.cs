using System;
using Wisej.Web;

namespace GridKrafter.WisejDemo
{
    /// <summary>
    /// A form to try AgingBuckets on: open it in the designer to see the component, or show it
    /// from your application to try it live.
    /// </summary>
    public partial class AgingBucketsTestForm : Form
    {
        public AgingBucketsTestForm()
        {
            InitializeComponent();

            this.agingBuckets1.Buckets = new[]
            {
                new AgingBucket("Current", 2840000, 112),
                new AgingBucket("1–30", 1120000, 48),
                new AgingBucket("31–60", 610000, 21),
                new AgingBucket("61–90", 240000, 9),
                new AgingBucket("90+", 135000, 4)
            };
            this.agingBuckets1.BucketSelected += this.agingBuckets1_BucketSelected;
        }

        // What the component fires is shown in labelEvents.
        private void agingBuckets1_BucketSelected(object sender, AgingBucketsBucketEventArgs e)
        {
            this.labelEvents.Text = "BucketSelected: " + e.Bucket;
        }
    }
}
