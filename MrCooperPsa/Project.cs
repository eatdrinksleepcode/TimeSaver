namespace MrCooperPsa
{
    public struct Project
    {
        public static Project HomeIntelligence = new Project
        {
            Id = "F1FBC909-CC8C-E711-811D-E0071B66DF51",
            Type = "10114",
            Name = "Home Intelligence"
        };

        public string Id { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
    }
}
