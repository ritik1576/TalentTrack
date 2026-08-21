using System;
using System.Collections.Generic;

namespace TalentTrack.Models.DTOs
{
    public class PagedList<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public PaginationMetadata Metadata { get; set; } = new PaginationMetadata();

        public PagedList() { }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            Items = items;
            Metadata = new PaginationMetadata
            {
                TotalItems = count,
                PageSize = pageSize,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(count / (double)pageSize)
            };
        }
    }
}
