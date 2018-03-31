namespace OK_RealState.Models
{
    public class AgencyLine
    {
        public string Text { get; set; }
        public string Value{ get; set; }

        public AgencyLine(string text, string value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}