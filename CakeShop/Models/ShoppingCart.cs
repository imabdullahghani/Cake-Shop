﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CakeShop.Models
{
    public class ShoppingCart : IShoppingCart
    {
        private readonly CakeShopDbContext _context;

        public string Id { get; set; }
        //public IEnumerable<ShoppingCartItem> ShoppingCartItems { get; set; }

        private ShoppingCart(CakeShopDbContext context)
        {
            _context = context;
        }

        public static ShoppingCart GetCart(IServiceProvider services)
        {
            var session = services.GetRequiredService<IHttpContextAccessor>()?.HttpContext.Session;
            var context = services.GetRequiredService<CakeShopDbContext>();
            var cardId = session.GetString("CartId") ?? Guid.NewGuid().ToString();

            session.SetString("CartId", cardId);

            return new ShoppingCart(context)
            {
                Id = cardId
            };
        }

        public async Task<int> AddToCartAsync(Cake cake, int qty = 1)
        {
            return await AddOrRemoveCart(cake, qty);

        }

        public async Task<int> RemoveFromCartAsync(Cake cake)
        {
            return await AddOrRemoveCart(cake, -1);
        }

        public async Task<IEnumerable<ShoppingCartItem>> GetShoppingCartItemsAsync()
        {
            return await _context.ShoppingCartItems
                .Where(e => e.ShoppingCartId == Id)
                .Include(e => e.Cake)
                .ToListAsync();
        }

        public async Task ClearCartAsync()
        {
            var shoppingCartItems = _context
                .ShoppingCartItems
                .Where(s => s.ShoppingCartId == Id);

            _context.ShoppingCartItems.RemoveRange(shoppingCartItems);

            await _context.SaveChangesAsync();
        }

        private async Task<int> AddOrRemoveCart(Cake cake, int qty)
        {
            var shoppingCartItem = await _context.ShoppingCartItems
                            .SingleOrDefaultAsync(s => s.CakeId == cake.Id && s.ShoppingCartId == Id);

            if (shoppingCartItem == null)
            {
                shoppingCartItem = new ShoppingCartItem
                {
                    ShoppingCartId = Id,
                    Cake = cake,
                    Qty = 0
                };

                await _context.ShoppingCartItems.AddAsync(shoppingCartItem);
            }

            shoppingCartItem.Qty += qty;

            if (shoppingCartItem.Qty <= 0)
            {
                shoppingCartItem.Qty = 0;
                _context.ShoppingCartItems.Remove(shoppingCartItem);
            }

            await _context.SaveChangesAsync();

            return await Task.FromResult(shoppingCartItem.Qty);
        }

    }
}
