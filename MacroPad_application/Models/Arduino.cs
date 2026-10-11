namespace MacroPad_application.Models;

public record Arduino(
    string Port,
    string Id
    )
{
    /// <summary>
    /// Checks whether an ID contains exactly six printable ASCII characters and is assigned to a device.
    /// </summary>
    /// <param name="id">The ID to validate, preserving spaces, punctuation, and leading zeroes.</param>
    /// <returns>True for six printable ASCII characters other than the unconfigured value 000000.</returns>
    public static bool IsValidId(string? id)
    {
        if (id == null)
        {
            return false;
        }

        bool valid = id.Length == 6 && id != "000000";
        
        for (int i = 0; valid && i < id.Length; i++)
        {
            valid = id[i] >= ' ' && id[i] <= '~';
        }

        return valid;
    }
}
