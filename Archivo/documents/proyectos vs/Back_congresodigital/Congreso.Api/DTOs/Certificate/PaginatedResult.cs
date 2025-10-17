namespace Congreso.Api.DTOs.Certificate;

/// <summary>
/// Resultado paginado de certificados
/// </summary>
public class PaginatedResult<T>
{
    /// <summary>
    /// Lista de elementos
    /// </summary>
    public List<T> Items { get; set; } = new();
    
    /// <summary>
    /// Número total de elementos
    /// </summary>
    public int TotalCount { get; set; }
    
    /// <summary>
    /// Número de página actual
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Tamaño de página
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Número total de páginas
    /// </summary>
    public int TotalPages { get; set; }
    
    /// <summary>
    /// Indica si hay página siguiente
    /// </summary>
    public bool HasNextPage { get; set; }
    
    /// <summary>
    /// Indica si hay página anterior
    /// </summary>
    public bool HasPreviousPage { get; set; }
}