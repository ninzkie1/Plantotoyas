﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using MoralesFiFthCRUD.ViewModels;
using MoralesFiFthCRUD.Repository;
using MoralesFiFthCRUD.Contracts;
using System.Data.Entity;
using System.Security.Cryptography;


namespace MoralesFiFthCRUD.Controllers
{

    public class HomeController : BaseController
    {

        private readonly database2Entities4 _dbContext;
        private readonly MailManager _mailManager;

        public HomeController()
        {
            _dbContext = new database2Entities4();
            _mailManager = new MailManager();
        }

        public ActionResult Index()
        {
            return View(_userRepo.GetAll());
        }

        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Login");
            return View();
        }
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(User u)
        {
            if (User.Identity.IsAuthenticated)
            {
                // Redirect to the Shop page
                return RedirectToAction("Dashboard");
            }
            var user = _userRepo._table.Where(m => m.username == u.username).FirstOrDefault();
            
            if (user != null)
            {
                if (user.password == u.password)
                {
                    FormsAuthentication.SetAuthCookie(u.username, false);
                    return RedirectToAction("Dashboard");
                }
            }
            ModelState.AddModelError("", "User not Exist or Incorrect Password");

            return View(u);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(User u, string SelectedRole)
        {
            _userRepo.Create(u);

            var userAdded = _userRepo._table.FirstOrDefault(m => m.username == u.username);

            if (userAdded == null)
            {
                // Handle case where user creation failed
                ModelState.AddModelError("", "Failed to create user.");
                return View(u); // Redisplay the form with an error message
            }

            if (string.IsNullOrEmpty(SelectedRole))
            {
                // Handle case where role is not selected
                ModelState.AddModelError("", "Role not selected.");
                return View(u); // Redisplay the form with an error message
            }

            var role = _db.Role.FirstOrDefault(r => r.roleName == SelectedRole);

            if (role == null)
            {
                // Handle case where role is not found (invalid selection)
                ModelState.AddModelError("", "Invalid role selected.");
                return View(u); // Redisplay the form with an error message
            }

            var userRole = new UserRole
            {
                userId = userAdded.id,
                roleId = role.id // Assign the retrieved roleId
            };

            _userRole.Create(userRole);

            TempData["Msg"] = $"User {u.username} added!";
            return RedirectToAction("LandingPage");
        }


        [Authorize(Roles = "Admin,Buyer,Seller")]
        public ActionResult Edit(int id)
        {

            return View(_userRepo.Get(id));
        }
        [HttpPost]
        public ActionResult Edit(User u)
        {
            _userRepo.Update(u.id, u);
            TempData["Msg"] = $"User {u.username} updated!";

            return RedirectToAction("index");

        }

        public ActionResult Delete(int id)
        {
            _userRepo.Delete(id);
            TempData["Msg"] = $"User deleted!";
            return RedirectToAction("index");
        }
        public ActionResult LandingPage()
        {
            return View();
        }

        public ActionResult Dashboard()
        {
            return View();
        }
        public ActionResult About()
        {
            return View();
        }
        public ActionResult ContactUs()
        {
            return View();
        }


        public ActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        public ActionResult SignUp(User u, string SelectedRole, string otp)
        {
            var storedOTP = Session["GeneratedOTP"]?.ToString();

            if (string.IsNullOrEmpty(storedOTP))
            {
                ModelState.AddModelError("", "OTP expired or not found. Please try signing up again.");
                return View(u);
            }

            if (otp != storedOTP)
            {
                ModelState.AddModelError("", "Incorrect OTP. Please try again.");
                return View(u);
            }

            // OTP is correct, proceed with user creation
            var existingUser = _userRepo._table.FirstOrDefault(m => m.username == u.username);

            if (existingUser != null)
            {
                TempData["ErrorMsg"] = "Username already exists. Please choose a different username.";
                return RedirectToAction("SignUp");
            }

            // Proceed with user creation if the username is unique
            _userRepo.Create(u);

            var userAdded = _userRepo._table.FirstOrDefault(m => m.username == u.username);

            if (userAdded == null)
            {
                ModelState.AddModelError("", "Failed to create user.");
                return View(u);
            }

            if (string.IsNullOrEmpty(SelectedRole))
            {
                ModelState.AddModelError("", "Role not selected.");
                return View(u);
            }

            var role = _db.Role.FirstOrDefault(r => r.roleName == SelectedRole);

            if (role == null)
            {
                ModelState.AddModelError("", "Invalid role selected.");
                return View(u);
            }

            var userRole = new UserRole
            {
                userId = userAdded.id,
                roleId = role.id
            };

            _userRole.Create(userRole);

            Session.Remove("GeneratedOTP");

            TempData["SuccessMsg"] = $"User {u.username} added!";
            return RedirectToAction("LandingPage");
        }


        [HttpPost]
        public ActionResult GenerateOTP(string email)
        {

            string generatedOTP = "";
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[6];
                rng.GetBytes(tokenData);
                generatedOTP = string.Join("", tokenData.Select(b => (b % 10).ToString()));
            }


            Session["GeneratedOTP"] = generatedOTP;


            string errResponse = "";
            bool emailSent = _mailManager.SendEmail(email, "Your OTP", $"Your sign up OTP is: {generatedOTP}", generatedOTP, ref errResponse);


            if (emailSent)
            {
                return Json(new { success = true, message = "OTP sent successfully!" });
            }
            else
            {
                return Json(new { success = false, message = $"Failed to send OTP: {errResponse}" });
            }
        }

        [Authorize(Roles = "Seller")]
        public ActionResult SellerView()
        {
            string userName = User.Identity.Name;
            var user = _userRepo._table.FirstOrDefault(u => u.username == userName);
            if (user == null)
            {
                // Handle the case where the user is not found
                ModelState.AddModelError("Shop", "Home");
                return View();
            }
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Seller")]
        public ActionResult SellerView(string productName, int categoryId, HttpPostedFileBase productImage, string productDescription, decimal productPrice, int productQuantity)
        {
            // Get the username of the currently logged-in user
            string userName = User.Identity.Name;

            // Retrieve the user from the repository based on the username
            var user = _userRepo._table.FirstOrDefault(u => u.username == userName);

            if (user == null)
            {
                // Handle the case where the user is not found
                ModelState.AddModelError("Shop", "Home");
                return View();
            }

            // Retrieve the category from the database based on the categoryId provided in the form
            var category = _db.Category.FirstOrDefault(c => c.id == categoryId);

            if (category == null)
            {
                // Handle the case where the category is not found
                ModelState.AddModelError("", "Category not found.");
                return View(); // You may redirect to an error page or display an error message
            }

            // Check if a file was uploaded
            if (productImage == null || productImage.ContentLength == 0)
            {
                ModelState.AddModelError("", "Please select a product image.");
                return View(); // Return to the view to display the error message
            }

            // Check if the file type is valid
            if (!productImage.ContentType.StartsWith("image/"))
            {
                ModelState.AddModelError("", "Please upload a valid image file.");
                return View(); // Return to the view to display the error message
            }

            // Check if the file size is within the limit (e.g., 5MB)
            if (productImage.ContentLength > 5 * 1024 * 1024)
            {
                ModelState.AddModelError("", "The image size exceeds the limit (5MB). Please upload a smaller image.");
                return View(); // Return to the view to display the error message
            }

            // Read the file data and convert it to a byte array
            byte[] imageData;
            using (var binaryReader = new BinaryReader(productImage.InputStream))
            {
                imageData = binaryReader.ReadBytes(productImage.ContentLength);
            }

            // Create a new Product object with the provided data
            var product = new Products
            {
                ProductName = productName,
                CategoryId = categoryId,
                UserId = user.id,
                price = productPrice,
                description = productDescription,
                Quantity = productQuantity, // Assign the quantity
                ProductImg = imageData // Assign the image data
            };

            // Add the product to the repository
            _productRepo.Create(product);

            TempData["SuccessMsg"] = "Product added successfully!";

            return RedirectToAction("SellerView"); // You may redirect to the product list page or any other appropriate page
        }




        public ActionResult MessageUs()
        {
            return View();

        }
        [Authorize(Roles = "Buyer")]
        public ActionResult Userprofile()
        {
            return View();
        }
        [Authorize(Roles = "Seller")]
        public ActionResult Resellerprofile()
        {
            // Get the username of the currently logged-in user
            string userName = User.Identity.Name;

            // Retrieve the user from the repository based on the username
            var user = _dbContext.User.FirstOrDefault(u => u.username == userName);

            if (user == null)
            {
                return View("Error");
            }

            // Retrieve products associated with the user (assuming there's a relationship between users and products)
            var userProducts = _dbContext.Products.Where(p => p.UserId == user.id).ToList();

            // Map product data to view model
            var productViewModels = userProducts.Select(p => new ProductViewModel
            {
                ProductID = p.ProductID,
                ProductName = p.ProductName,
                Category = p.Category != null ? p.Category.CategoryName : "N/A",
                ProductImg = p.ProductImg,
                Description = p.description,
                Quantity = p.Quantity ?? 0,
                Price = p.price ?? 0,
                sellerName = p.User.username
            }).ToList();

            // Pass the productViewModels list to the view
            return View(productViewModels);
        }



        [AllowAnonymous]
        public ActionResult test(string username)
        {
            // Retrieve the user from the repository based on the username
            var user = _dbContext.User.FirstOrDefault(u => u.username == username);
            if (user == null)
            {
                return RedirectToAction("Shop", "Home");
            }

            var products = _dbContext.Products
                .Where(p => p.UserId == user.id && p.Category != null)
                .ToList() // Fetch the data from the database
                .Select(p => new ProductViewModel
                {
                    ProductID = p.ProductID,
                    ProductName = p.ProductName,
                    Category = p.Category.CategoryName,
                    ProductImg = p.ProductImg,
                    Description = p.description,
                    Quantity = p.Quantity ?? 0,
                    sellerName = p.User.username,
                    Price = p.price ?? 0,
                })
                .ToList();

            return View(products);
        }

        [Authorize(Roles = "Seller")]
        public ActionResult DeleteProduct(int id)
        {
            var result = _productRepo.Delete(id);

            if (result == ErrorCode.Success)
            {
                // Product deleted successfully
                TempData["SuccessMsg"] = "Product deleted successfully!";
            }
            else
            {
                // Failed to delete product
                TempData["ErrorMsg"] = "Failed to delete product.";
            }

            return RedirectToAction("ResellerProfile");
        }
        [HttpGet]
        [Authorize(Roles = "Seller")]
        public ActionResult EditProduct(int id)
        {
            var product = _dbContext.Products.Find(id);

            if (product == null)
            {
                TempData["ErrorMsg"] = "Product not found.";
                return RedirectToAction("Shop");
            }

            if (product.UserId != GetCurrentUserId())
            {
                TempData["ErrorMsg"] = "You are not authorized to edit this product.";
                return RedirectToAction("Shop");
            }

            var viewModel = new ProductViewModel
            {
                ProductID = product.ProductID,
                ProductName = product.ProductName,
                CategoryId = product.CategoryId ?? 0,
                Description = product.description,
                Quantity = product.Quantity ?? 0,
                Price = product.price ?? 0
                // ProductImg = product.ProductImg // If you're handling image edits
            };

            ViewBag.Categories = new SelectList(_dbContext.Category, "id", "CategoryName", viewModel.CategoryId);
            return View(viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Seller")]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(ProductViewModel productVM)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_dbContext.Category, "id", "CategoryName", productVM.CategoryId);
                return View(productVM);
            }

            var product = _dbContext.Products.Find(productVM.ProductID);

            if (product == null)
            {
                TempData["ErrorMsg"] = "Product not found.";
                return RedirectToAction("Shop");
            }

            if (product.UserId != GetCurrentUserId())
            {
                TempData["ErrorMsg"] = "You are not authorized to edit this product.";
                return RedirectToAction("Shop");
            }

            product.ProductName = productVM.ProductName;
            product.CategoryId = productVM.CategoryId;
            product.description = productVM.Description;
            product.Quantity = productVM.Quantity;
            product.price = productVM.Price;

            // If butangan og Image
            // if (productVM.ProductImg != null)
            // {
            //     product.ProductImg = productVM.ProductImg;
            // }

            _dbContext.Entry(product).State = EntityState.Modified;

            try
            {
                _dbContext.SaveChanges();
                TempData["SuccessMsg"] = "Product updated successfully!";
                return RedirectToAction("ResellerProfile");
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Failed to update product: " + ex.Message;
                return RedirectToAction("ResellerProfile");
            }
        }



        // Helper method to get the current user's ID
        private int GetCurrentUserId()
        {
            string userName = User.Identity.Name;
            var user = _userRepo._table.FirstOrDefault(u => u.username == userName);
            return user?.id ?? 0; // Return 0 if user not found (handle appropriately in your app)
        }




        //BUY Dire

        public ActionResult Buy(int productId, int quantity)
        {
            string buyerName = User.Identity.Name;

            var buyer = _dbContext.User.FirstOrDefault(u => u.username == buyerName);

            if (buyer == null)
            {
                TempData["ErrorMsg"] = "User not found.";
                return RedirectToAction("Shop");
            }

            var product = _dbContext.Products.FirstOrDefault(p => p.ProductID == productId);

            if (product == null)
            {
                TempData["ErrorMsg"] = "Product not found.";
                return RedirectToAction("Shop");
            }

            if (product.Quantity < quantity)
            {
                TempData["ErrorMsg"] = "Insufficient quantity available.";
                return RedirectToAction("Shop");
            }

            // Look for an existing purchase record in PurchasedProducts
            var existingBoughtProduct = _dbContext.Cart.FirstOrDefault(p =>
                p.UserId == buyer.id && p.ProductID == productId);

            if (existingBoughtProduct != null)
            {
                existingBoughtProduct.Quantity += quantity;
                existingBoughtProduct.Price = existingBoughtProduct.Quantity * product.price;
            }
            else
            {
                var boughtProduct = new Cart
                {

                    ProductID = product.ProductID,
                    UserId = buyer.id,
                    BuyerName = buyerName,
                    Quantity = quantity,
                    Price = product.price,
                    CategoryName = product.Category.CategoryName,
                    ProductName = product.ProductName,
                    description = product.description,
                    ProductImg = product.ProductImg

                    // Add other relevant fields if needed
                };

                _dbContext.Cart.Add(boughtProduct);
            }
            if (product.SoldOut)
            {
                TempData["ErrorMsg"] = "This product is sold out.";
                return RedirectToAction("Shop");
            }

            product.Quantity -= quantity; // Update product inventory

            if (product.Quantity == 0)
            {
                product.SoldOut = true;
            }

            _dbContext.SaveChanges();

            TempData["SuccessMsg"] = "Purchase successful!";
            return RedirectToAction("Shop");
        }

        public ActionResult Shop(string searchTerm, string sellerName)
        {

            var products = _dbContext.Products.ToList();


            if (!string.IsNullOrWhiteSpace(searchTerm))
            {

                products = products.Where(p => p.ProductName.Contains(searchTerm)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(sellerName))
            {
                products = products.Where(p => p.User.username == sellerName).ToList();
            }

            var sellerProducts = products.Where(p => p.User.UserRole.Any(r => r.Role.roleName == "Seller"))
                    .Select(p => new ProductViewModel
                    {

                        ProductID = p.ProductID,
                        ProductName = p.ProductName,
                        Category = p.Category != null ? p.Category.CategoryName : "N/A",
                        ProductImg = p.ProductImg,
                        Description = p.description,
                        Quantity = p.Quantity ?? 0,
                        Price = p.price ?? 0,
                        sellerName = p.User.username
                    })
                     .ToList();

            // Pass the list of seller products to the view
            return View(sellerProducts);
        }

        [Authorize(Roles = "Buyer")]
        public ActionResult ViewCart()
        {
            string buyerName = User.Identity.Name;

            var buyer = _dbContext.User.FirstOrDefault(u => u.username == buyerName);

            if (buyer == null)
            {
                return View("Error");
            }

            // Get the buyer's cart items
            var boughtProducts = _dbContext.Cart
                .Where(p => p.UserId == buyer.id)
                .Include(p => p.Products) // Include the related product
                .ToList()
                .Select(p => new ProductViewModel
                {
                    ProductID = p.Products.ProductID,
                    ProductName = p.Products.ProductName,  // Access from Products
                    Category = p.Products.Category != null ? p.Products.Category.CategoryName : "N/A",
                    ProductImg = p.Products.ProductImg,
                    Description = p.Products.description,
                    Quantity = p.Quantity ?? 0,
                    Price = p.Price ?? 0,
                    sellerName = p.Products.User.username, // Assuming you want the seller's name
                    BuyerName = buyerName // Add buyer's username 
                })
                .ToList();

            return View(boughtProducts);
        }


        [HttpPost]
        public ActionResult DecrementQuantity(int productId)
        {
            // Retrieve the product from the cart
            var productInCart = _dbContext.Cart.FirstOrDefault(p => p.ProductID == productId);
            if (productInCart != null)
            {
                // Decrement the quantity in the cart
                productInCart.Quantity--;

                // Check if the quantity becomes 0
                if (productInCart.Quantity <= 0)
                {
                    // Remove the product from the cart
                    _dbContext.Cart.Remove(productInCart);
                }

                // Calculate the total price to be subtracted
                decimal pricePerUnit = productInCart.Price ?? 0;
                decimal totalPriceToSubtract = pricePerUnit;

                // Update the cart in the database
                _dbContext.SaveChanges();

                // Retrieve the corresponding product from the inventory
                var productInInventory = _dbContext.Products.FirstOrDefault(p => p.ProductID == productId);
                if (productInInventory != null)
                {
                    // Adjust the product price in the inventory based on the decremented quantity
                    productInInventory.price -= totalPriceToSubtract;

                    // Update the inventory in the database
                    _dbContext.SaveChanges();
                }
            }

            // Redirect back to the cart page or wherever you want to go after decrementing the quantity
            return RedirectToAction("ViewCart");
        }


    }

}


