﻿using ECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ECommerce.Models.Orders;
using Microsoft.AspNetCore.Identity;
using ECommerce.Models.Users;
using ECommerce.Models._Enums;
namespace ECommerce.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly StoreContext _db;
        private  List<ShoppingCartItem> _items;
        private readonly UserManager<User> _userManager;
        public ShoppingCartController(StoreContext context, UserManager<User> userManager)
        {
            _db = context;
            _items = new List<ShoppingCartItem>();
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult AddtoCart(int id)
        {
            var _product=_db.Products.Find(id);
            var _cartItems=HttpContext.Session.Get<List<ShoppingCartItem>>("Cart")??new List<ShoppingCartItem>();
            var extingproduct = _cartItems.FirstOrDefault(x => x.product.Id == id);
            var count = HttpContext.Session.Get<int>("count");
            if (_product.QuantityInStock <= 0 || _product.IsActive==false)
            {
                return RedirectToAction("Details","Products", new
                {
                    ID = id
                });
            }
            if (extingproduct != null)
            {
                if (_product.QuantityInStock > extingproduct.Quantity)
                { extingproduct.Quantity++; }
                else
                {
                    return RedirectToAction("Details","Products", new
                    {
                        ID = id
                    });
                }
            }
            else
            {
                _cartItems.Add(new ShoppingCartItem
                {
                    Quantity = 1,

                    product = _product
                });
            }
            count++;
            HttpContext.Session.Set("Cart", _cartItems);
            HttpContext.Session.Set("count", count);
            return RedirectToAction("Index", "Products");

        }
        public IActionResult ViewCart()
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();

            var catviewmodel = new ShoppingCartViewItem
            {
                Items = _cartItems,
                TotalPrice = _cartItems.Sum(item => item.product.Price * item.Quantity)
            };
            return View(catviewmodel);
        }
        public IActionResult RemoveFromCart(int id)
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            var extingproduct = _cartItems.FirstOrDefault(x => x.product.Id == id);
            var count = HttpContext.Session.Get<int>("count");
            if (extingproduct != null)
            {
                _cartItems.Remove(extingproduct);
                count -=extingproduct.Quantity;
            }
            HttpContext.Session.Set("Cart", _cartItems);
            HttpContext.Session.Set("count", count);
            return RedirectToAction("ViewCart");
        }
        
        public async Task<IActionResult> Purchase()
        {
            var _cartItems = HttpContext.Session.Get<List<ShoppingCartItem>>("Cart") ?? new List<ShoppingCartItem>();
            if(_cartItems.Count==0)
            {
                return RedirectToAction("Index", "Products");
            }
            var orderitemincart = new List<OrderItem>();
            decimal sumtotal = 0;
            
            foreach (var item in _cartItems)
            {
                var product = _db.Products.Find(item.product.Id);
                product.QuantityInStock -= item.Quantity;
                orderitemincart.Add(new OrderItem
                {
                    Id = item.Id,
                    ProductId=item.product.Id,
                    Quantity=item.Quantity,
                    //OrderId=1,
                    UnitPrice=Convert.ToDecimal(item.product.Price)
                });
                sumtotal =sumtotal + Convert.ToDecimal(item.product.Price * item.Quantity);
                _db.OrderItems.AddRange(orderitemincart);
            }
            var totals = new Order();
            var user = await _userManager.GetUserAsync(User);
            totals.UserId = user.Id;
            totals.OrderDate = DateTime.Now;
            totals.OrderItems= orderitemincart;
            totals.TotalAmount=sumtotal;
            totals.Status=OrderStatus.Pending;
            totals.CompanyShippingId = 1;
            _db.Orders.Add(totals);
            _db.SaveChanges();
            OrderStatusHistory stat=new OrderStatusHistory();
            stat.OrderId=totals.Id;
            stat.UserId= user.Id;
            stat.ChangeDate= DateTime.Now;
            stat.OldStatus = OrderStatus.Pending;
            stat.NewStatus = OrderStatus.Pending;
            _db.OrderStatusHistories.Add(stat);
            _db.SaveChanges();
            HttpContext.Session.Set("Cart", new List<ShoppingCartItem>());
            HttpContext.Session.Set("count", 0);
            return RedirectToAction("Index","Home");
        }
    }
}
