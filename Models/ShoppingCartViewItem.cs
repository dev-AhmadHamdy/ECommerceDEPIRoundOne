﻿using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Models
{
    [NotMapped]

    public class ShoppingCartViewItem
    {
        public List<ShoppingCartItem> Items { get; set; }
        public decimal? TotalPrice { get; set;}
        public int? TotalQuantity { get; set;}
    }
}
