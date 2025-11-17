using System.ComponentModel.DataAnnotations;

namespace Rag.UI.Models;

public class SearchFormModel
{
    [Required(ErrorMessage = "Please enter a search query")]
    public string Query { get; set; } = string.Empty;
}