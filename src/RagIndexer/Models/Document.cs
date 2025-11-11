namespace RagIndexer.Models;

public record Document(
    Guid Id,
    string Title,
    string Author,
    string Content,
    int PageNumber
);

    
