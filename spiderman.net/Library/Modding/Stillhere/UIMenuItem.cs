using System.Collections.Generic;
using GTA;

namespace SpiderMan.Library.Modding.Stillhere
{
    public class UIMenuItem
    {
        public List<string> DescriptionTexts;

        public UIMenuItem(string text)
        {
            _text = text;
        }

        public UIMenuItem(string text, dynamic value)
        {
            _text = text;
            _value = value;
        }

        public UIMenuItem(string text, dynamic value, string description)
        {
            _text = text;
            _value = value;
            _description = description;
            //DescriptionTexts = description.SplitOn(90);

            if (_description != null)
                DescriptionWidth = StringHelper.MeasureStringWidth(_description, Font.ChaletComprimeCologne, 0.452f);
        }

        private string _text { get; set; }
        private dynamic _value { get; set; }
        private string _description { get; set; }
        public float DescriptionWidth { get; set; }
        public object Tag { get; set; }

        public string Text
        {
            get => _text;
            set => _text = value;
        }

        public dynamic Value
        {
            get => _value;
            set => _value = value;
        }

        public string Description
        {
            get => _description;
            set
            {
                //DescriptionTexts = value.SplitOn(90);

                if (value != null)
                    DescriptionWidth = StringHelper.MeasureStringWidth(value, Font.ChaletComprimeCologne, 0.452f);

                _description = value;
            }
        }

        public virtual void ChangeListIndex()
        {
        }
    }
}