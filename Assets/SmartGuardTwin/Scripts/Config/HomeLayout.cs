using UnityEngine;

namespace SmartGuardTwin.Config
{
    /// <summary>
    /// Replica of the "Experiment'Haal" studio from Bouchabou et al. (Sensors 2023,
    /// Figures 2 and 3): the same 6-room arrangement and proportions, scaled up ~1.5x
    /// for more space between furniture, and with interior walls + doorways added.
    ///
    /// Coordinates use the PAPER convention: origin at the NW corner,
    ///   x = east  (0 .. Width),  z = south-from-north (0 .. Depth).
    /// SceneBuilder converts z to world space (worldZ = Depth - z) so the top-down view
    /// is oriented like the paper (north at the top). All distances are metres.
    /// </summary>
    public static class HomeLayout
    {
        public const float Width = 11.5f;   // east-west
        public const float Depth = 6.30f;   // north-south
        public const float WallHeight = 1.2f;
        public const float WallThick = 0.10f;
        public const float DoorWidth = 1.10f;

        // Material palette (declared before Furniture so static init order is correct).
        static readonly Color Wood = new Color(0.55f, 0.40f, 0.26f);
        static readonly Color Appliance = new Color(0.82f, 0.82f, 0.85f);
        static readonly Color Fabric = new Color(0.35f, 0.45f, 0.65f);
        static readonly Color White = new Color(0.92f, 0.92f, 0.94f);
        static readonly Color Dark = new Color(0.20f, 0.20f, 0.22f);

        public struct Room
        {
            public string name;
            public float x0, x1, z0, z1;
            public Color color;
            public Room(string n, float x0, float x1, float z0, float z1, Color c)
            { name = n; this.x0 = x0; this.x1 = x1; this.z0 = z0; this.z1 = z1; color = c; }
            public float Cx => (x0 + x1) * 0.5f;
            public float Cz => (z0 + z1) * 0.5f;
        }

        public enum Shape { Box, Cylinder }

        public struct Item
        {
            public string id, room;
            public Shape shape;
            public float x, z;        // paper coords, object centre
            public float w, d, h;     // w along X (east), d along Z, h vertical
            public Color color;
            public Item(string id, string room, Shape s, float x, float z, float w, float d, float h, Color c)
            { this.id = id; this.room = room; shape = s; this.x = x; this.z = z; this.w = w; this.d = d; this.h = h; color = c; }
        }

        /// <summary>A wall line. Vertical: x=line, spans z in [a,b]. Horizontal: z=line, spans x in [a,b].</summary>
        public struct WallLine { public bool vertical; public float line, a, b; }

        /// <summary>A doorway opening linking two rooms, at paper coords (x,z). Used to gap
        /// walls here AND to route the avatar through doors in Phase 3.</summary>
        public struct DoorLink { public string a, b; public float x, z;
            public DoorLink(string a, string b, float x, float z) { this.a = a; this.b = b; this.x = x; this.z = z; } }

        // 6 rooms (colours match the Figure 3 legend), scaled ~1.5x from the paper.
        public static readonly Room[] Rooms =
        {
            new Room("entrance",    0.00f,  2.30f, 0.00f, 2.40f, new Color(0.78f, 0.88f, 0.78f)), // green
            new Room("bedroom",     2.30f,  7.30f, 0.00f, 2.40f, new Color(0.98f, 0.86f, 0.62f)), // orange
            new Room("kitchen",     7.30f, 11.50f, 0.00f, 3.80f, new Color(0.98f, 0.84f, 0.86f)), // pink
            new Room("livingroom",  0.00f,  3.80f, 2.40f, 6.30f, new Color(0.99f, 0.96f, 0.72f)), // yellow
            new Room("dining_room", 3.80f,  7.30f, 2.40f, 6.30f, new Color(0.86f, 0.80f, 0.93f)), // purple
            new Room("bathroom",    7.30f, 11.50f, 3.80f, 6.30f, new Color(0.80f, 0.87f, 0.96f)), // blue
        };

        // 'outside' pad north of the entrance (beyond the front door).
        public static readonly Room Outside =
            new Room("outside", 0.00f, 2.30f, -1.60f, 0.00f, new Color(0.55f, 0.60f, 0.55f));

        // Walls: perimeter + interior. Doorways (below) are cut out automatically.
        public static readonly WallLine[] Walls =
        {
            // Perimeter
            new WallLine { vertical = true,  line = 0.00f,  a = 0.00f, b = Depth },  // west
            new WallLine { vertical = true,  line = Width,  a = 0.00f, b = Depth },  // east
            new WallLine { vertical = false, line = 0.00f,  a = 0.00f, b = Width },  // north (front door)
            new WallLine { vertical = false, line = Depth,  a = 0.00f, b = Width },  // south
            // Interior partitions
            new WallLine { vertical = true,  line = 2.30f,  a = 0.00f, b = 2.40f },  // entrance | bedroom
            new WallLine { vertical = true,  line = 3.80f,  a = 2.40f, b = Depth },  // livingroom | dining
            new WallLine { vertical = true,  line = 7.30f,  a = 0.00f, b = Depth },  // bedroom/dining | kitchen/bathroom
            new WallLine { vertical = false, line = 2.40f,  a = 0.00f, b = 7.30f },  // (entrance,bedroom) | (living,dining)
            new WallLine { vertical = false, line = 3.80f,  a = 7.30f, b = Width },  // kitchen | bathroom
        };

        // Doorways linking the rooms (also feeds the avatar's navigation graph in Phase 3).
        public static readonly DoorLink[] Doorways =
        {
            new DoorLink("outside",     "entrance",    1.00f, 0.00f),
            new DoorLink("entrance",    "bedroom",     2.30f, 1.80f),
            new DoorLink("entrance",    "livingroom",  1.10f, 2.40f),
            new DoorLink("bedroom",     "dining_room", 5.50f, 2.40f),
            new DoorLink("bedroom",     "kitchen",     7.30f, 1.20f),
            new DoorLink("livingroom",  "dining_room", 3.80f, 5.00f),
            new DoorLink("dining_room", "bathroom",    7.30f, 5.00f),
            new DoorLink("kitchen",     "bathroom",    9.00f, 3.80f),
        };

        // Representative furniture, spread out within the larger rooms. Ids double as
        // object ids for the sensor/interaction layers.
        public static readonly Item[] Furniture =
        {
            // Entrance
            new Item("front_door",      "entrance",    Shape.Box,      1.00f, 0.05f, 0.90f, 0.08f, 2.00f, Wood),

            // Bedroom
            new Item("bed",             "bedroom",     Shape.Box,      3.70f, 1.00f, 2.00f, 1.40f, 0.50f, Fabric),
            new Item("nightstand",      "bedroom",     Shape.Box,      2.80f, 0.40f, 0.40f, 0.40f, 0.50f, Wood),
            new Item("wardrobe",        "bedroom",     Shape.Box,      6.70f, 0.50f, 0.80f, 0.55f, 2.00f, Wood),
            new Item("medication",      "bedroom",     Shape.Box,      5.60f, 0.40f, 0.25f, 0.25f, 0.30f, new Color(0.85f, 0.25f, 0.25f)),

            // Kitchen
            new Item("fridge",          "kitchen",     Shape.Box,      7.90f, 0.50f, 0.70f, 0.70f, 1.80f, Appliance),
            new Item("dishwasher",      "kitchen",     Shape.Box,      9.20f, 0.50f, 0.60f, 0.60f, 0.85f, Appliance),
            new Item("kitchen_cabinet", "kitchen",     Shape.Box,     10.30f, 0.45f, 0.90f, 0.40f, 0.70f, Wood),
            new Item("stove",           "kitchen",     Shape.Box,     11.10f, 1.00f, 0.45f, 0.60f, 0.92f, Dark),
            new Item("oven",            "kitchen",     Shape.Box,     11.10f, 1.80f, 0.45f, 0.58f, 0.70f, Dark),
            new Item("kettle",          "kitchen",     Shape.Cylinder,11.00f, 2.50f, 0.18f, 0.18f, 0.26f, Appliance),
            new Item("microwave",       "kitchen",     Shape.Box,     10.90f, 3.10f, 0.45f, 0.35f, 0.30f, Dark),
            new Item("kitchen_sink",    "kitchen",     Shape.Box,     11.10f, 3.50f, 0.45f, 0.45f, 0.20f, Appliance),

            // Livingroom
            new Item("sofa",            "livingroom",  Shape.Box,      1.70f, 5.90f, 1.80f, 0.80f, 0.70f, Fabric),
            new Item("coffee_table",    "livingroom",  Shape.Box,      1.70f, 4.80f, 0.90f, 0.50f, 0.40f, Wood),
            new Item("tv_stand",        "livingroom",  Shape.Box,      1.70f, 2.70f, 1.20f, 0.40f, 0.50f, Wood),
            new Item("tv",              "livingroom",  Shape.Box,      1.70f, 2.62f, 1.00f, 0.10f, 0.60f, Dark),
            new Item("armchair",        "livingroom",  Shape.Box,      3.20f, 4.20f, 0.80f, 0.80f, 0.70f, Fabric),

            // Dining room
            new Item("dining_table",    "dining_room", Shape.Cylinder, 5.50f, 4.50f, 1.25f, 1.25f, 0.75f, Wood),
            new Item("chair_1",         "dining_room", Shape.Box,      4.50f, 4.50f, 0.40f, 0.40f, 0.50f, Wood),
            new Item("chair_2",         "dining_room", Shape.Box,      6.50f, 4.50f, 0.40f, 0.40f, 0.50f, Wood),
            new Item("chair_3",         "dining_room", Shape.Box,      5.50f, 3.50f, 0.40f, 0.40f, 0.50f, Wood),
            new Item("chair_4",         "dining_room", Shape.Box,      5.50f, 5.50f, 0.40f, 0.40f, 0.50f, Wood),

            // Bathroom
            new Item("bath",            "bathroom",    Shape.Box,      8.70f, 5.90f, 1.60f, 0.70f, 0.60f, White),
            new Item("toilet",          "bathroom",    Shape.Box,     11.00f, 5.90f, 0.45f, 0.55f, 0.45f, White),
            new Item("bath_sink",       "bathroom",    Shape.Box,      7.90f, 4.30f, 0.50f, 0.40f, 0.85f, White),
        };
    }
}
