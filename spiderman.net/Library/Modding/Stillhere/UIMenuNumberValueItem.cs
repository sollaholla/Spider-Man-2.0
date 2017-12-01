using GTA;

namespace SpiderMan.Library.Modding.Stillhere
{
    public class UIMenuNumberValueItem : UIMenuItem
    {
        public UIMenuNumberValueItem(string text, dynamic value) : base(text, (object) value)
        {
            this.Text = text;
            this.Value = "< " + value + " >";
        }

        public UIMenuNumberValueItem(string text, dynamic value, string description) : base(text, (object) value,
            description)
        {
            this.Text = text;
            this.Value = "< " + value + " >";
            this.Description = description;
            //DescriptionTexts = description.SplitOn(90);

            if (description != null)
                DescriptionWidth = StringHelper.MeasureStringWidth(description, Font.ChaletComprimeCologne, 0.452f);
        }
    }
}