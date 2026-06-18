using UnityEngine;

namespace SmartGuardTwin.Home
{
    /// <summary>
    /// Tags a floor quad with its room name. The zone-occupancy sensor (Phase 4)
    /// casts a ray straight down from the avatar and reads <see cref="RoomName"/>
    /// off whichever floor it hits — exactly the "sensitive floor" mechanism from
    /// the paper (Section 3.1).
    /// </summary>
    public class RoomZone : MonoBehaviour
    {
        public string RoomName;
    }
}
