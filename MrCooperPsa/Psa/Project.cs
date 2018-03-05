namespace MrCooperPsa.Psa
{
    public struct Project
    {
        public static Project HomeIntelligence = new Project
        {
            Id = "F1FBC909-CC8C-E711-811D-E0071B66DF51",
            Type = "10114",
            Name = "Home Intelligence",
            DevelopmentTask = Task.HomeIntelligence_Development,
        };

        public static Project MyWay = new Project
        {
            Id = "82D29622-3779-E711-8116-E0071B6AA0D1",
            Type = "10114",
            Name = "MyWay - Digital",
            DevelopmentTask = Task.MyWay_Development,
        };

        public string Id { get; private set; }
        public string Type { get; private set; }
        public string Name { get; private set; }
        public Task DevelopmentTask { get; set; }
    }
}
