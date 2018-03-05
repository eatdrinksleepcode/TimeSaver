namespace MrCooperPsa.Psa {
    public struct Task
    {
        public static readonly Task HomeIntelligence_Development = new Task
        {
            Id = "22E5EC7A-86EF-46DF-B392-A97AFD816232",
            Name = "4. Development",
        };

        public static readonly Task MyWay_Development = new Task
        {
            Id = "1280CB62-965F-4205-ACFE-2F96B0757F50",
            Name = "Development",
        };

        public string Id { get; set; }
        public string Name { get; set; }
    }
}