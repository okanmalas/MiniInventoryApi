namespace MiniInventoryApi.Models;

public class Kategori
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Stok> Urunler { get; set; } = new List<Stok>();
}