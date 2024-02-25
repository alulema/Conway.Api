using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace Conway.Api.Models;

public class GameBoard
{
    public Guid Id { get; set; }
    public string StateSerialized { get; set; }
    public int Generation { get; set; } = 0;

    [NotMapped]
    public bool[,] State
    {
        get => JsonConvert.DeserializeObject<bool[,]>(StateSerialized);
        set => StateSerialized = JsonConvert.SerializeObject(value);
    }
}
