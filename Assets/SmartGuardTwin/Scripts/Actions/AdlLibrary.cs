using static SmartGuardTwin.Actions.ActionStep;

namespace SmartGuardTwin.Actions
{
    /// <summary>
    /// The paper's Activities of Daily Living (Bouchabou 2023, §3.2.2), each encoded as a
    /// parameterized <see cref="ActionProgram"/> over the home's objects. The avatar
    /// executing these is what makes the sensors fire in characteristic patterns.
    /// </summary>
    public static class AdlLibrary
    {
        public static readonly string[] Labels =
        {
            "Bathe", "Cook breakfast", "Cook lunch", "Cook dinner", "Dress",
            "Eat breakfast", "Eat lunch", "Eat dinner", "Enter home", "Go to toilet",
            "Leave home", "Read", "Sleep", "Sleep in Bed", "Wash dishes", "Watch TV", "Take medication"
        };

        public static ActionProgram Build(string label)
        {
            switch (label)
            {
                case "Bathe": return new ActionProgram(label)
                    .Add(Go("bath")).Add(LightOn("bathroom")).Add(Wait(480)).Add(LightOff("bathroom"));

                case "Cook breakfast": return new ActionProgram(label)
                    .Add(LightOn("kitchen"))
                    .Add(Go("fridge")).Add(Open("fridge")).Add(Wait(20)).Add(Close("fridge"))
                    .Add(Go("kettle")).Add(On("kettle")).Add(Wait(120)).Add(Off("kettle"))
                    .Add(Go("stove")).Add(On("stove")).Add(Wait(180)).Add(Off("stove"));

                case "Cook lunch": return new ActionProgram(label)
                    .Add(LightOn("kitchen"))
                    .Add(Go("fridge")).Add(Open("fridge")).Add(Wait(20)).Add(Close("fridge"))
                    .Add(Go("microwave")).Add(On("microwave")).Add(Wait(150)).Add(Off("microwave"))
                    .Add(Go("stove")).Add(On("stove")).Add(Wait(180)).Add(Off("stove"));

                case "Cook dinner": return new ActionProgram(label)
                    .Add(LightOn("kitchen"))
                    .Add(Go("fridge")).Add(Open("fridge")).Add(Wait(25)).Add(Close("fridge"))
                    .Add(Go("oven")).Add(On("oven")).Add(Wait(300)).Add(Off("oven"))
                    .Add(Go("stove")).Add(On("stove")).Add(Wait(240)).Add(Off("stove"));

                case "Dress": return new ActionProgram(label)
                    .Add(Go("wardrobe")).Add(Open("wardrobe")).Add(Wait(120)).Add(Close("wardrobe"));

                case "Eat breakfast": return Eat(label, "chair_1", 420);
                case "Eat lunch":     return Eat(label, "chair_2", 480);
                case "Eat dinner":    return Eat(label, "chair_3", 600);

                case "Enter home": return new ActionProgram(label)
                    .Add(GoRoom("outside")).Add(Open("front_door")).Add(GoRoom("entrance")).Add(Close("front_door"));

                case "Go to toilet": return new ActionProgram(label)
                    .Add(Go("toilet")).Add(Wait(120));

                case "Leave home": return new ActionProgram(label)
                    .Add(GoRoom("entrance")).Add(Open("front_door")).Add(GoRoom("outside")).Add(Close("front_door"));

                case "Read": return new ActionProgram(label)
                    .Add(LightOn("livingroom")).Add(Sit("sofa")).Add(Wait(720)).Add(Stand()).Add(LightOff("livingroom"));

                case "Sleep": return new ActionProgram(label)
                    .Add(LightOff("bedroom")).Add(Lie("bed")).Add(Wait(2400)).Add(Stand());

                case "Sleep in Bed": return new ActionProgram(label)
                    .Add(Lie("bed")).Add(Wait(1200)).Add(Stand());

                case "Wash dishes": return new ActionProgram(label)
                    .Add(Go("kitchen_sink")).Add(On("dishwasher")).Add(Wait(600)).Add(Off("dishwasher"));

                case "Watch TV": return new ActionProgram(label)
                    .Add(Sit("sofa")).Add(On("tv")).Add(Wait(1200)).Add(Off("tv")).Add(Stand());

                case "Take medication": return new ActionProgram(label)
                    .Add(Go("medication")).Add(Open("medication")).Add(Wait(30)).Add(Close("medication"));

                default: return null;
            }
        }

        static ActionProgram Eat(string label, string chair, float secs) => new ActionProgram(label)
            .Add(Sit(chair)).Add(Wait(secs)).Add(Stand());
    }
}
