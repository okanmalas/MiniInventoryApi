using System.Text.Json.Serialization;

namespace MiniInventoryApi.Models;

public class Stok
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Price { get; set; }
    public string Description { get; set; }
    
    public int KategoriId { get; set; }
    
    [JsonIgnore] 
    public Kategori? Kategori { get; set; }
    
}