using Rat6.Protocol;

namespace Rat6.Device;

public interface IRat6Device : IDisposable
{
    int GetFirmware();

    SelectedSlot GetSelectedSlot();

    Profile ReadProfile(int slot);

    IReadOnlyList<Profile> ReadAllProfiles();

    void WriteProfile(int slot, Profile profile);

    void Reload();

    void SelectSlot(int slot);

    string BackupAll(string? root = null);
}
