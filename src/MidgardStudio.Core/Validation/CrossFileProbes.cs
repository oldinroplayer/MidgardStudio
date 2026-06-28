using System.Collections.Generic;

namespace MidgardStudio.Core.Validation;

/// <summary>Read-only client-item facts a cross-file rule needs. The App adapter maps these from the live
/// client itemInfo model so the rules never touch the App service directly.</summary>
public sealed record ClientItemFacts(int SlotCount, int ClassNum, string? IdentifiedResourceName);

/// <summary>Read port: does the client itemInfo know this id, and what are its facts.</summary>
public interface IClientItemProbe
{
    bool Exists(int id);
    ClientItemFacts? Get(int id);
}

/// <summary>Read port: GRF inventory-icon presence (the adapter resolves the in-GRF path).</summary>
public interface IGrfIconProbe
{
    bool IsConfigured { get; }
    bool IconExists(string identifiedResourceName);
}

/// <summary>Read port: which headgear View ids are mapped in accessoryid.lub / accname.lub.</summary>
public interface IAccessoryMapProbe
{
    bool IsAvailable { get; }
    IReadOnlySet<int> MappedViewIds();
}

/// <summary>Write port for the item cross-file quick-fixes — the only place a rule mutates client state,
/// so Core sees a port, not the App service. The App adapter wraps the client itemInfo writer.</summary>
public interface IClientItemEditor
{
    void SetSlots(int id, int slots);
    void SetClassNum(int id, int classNum);
    void CreateText(int id, string name, int slots, int classNum);
    void Remove(int id);
}
