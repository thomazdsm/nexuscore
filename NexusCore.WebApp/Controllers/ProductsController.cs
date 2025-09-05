using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NexusCore.Application.DTOs;
using NexusCore.Application.Interfaces;
using NexusCore.WebApp.Models.Product;
using System;
using System.Threading.Tasks;

namespace NexusCore.WebApp.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly IMapper _mapper;

        public ProductsController(IProductService productService, IMapper mapper)
        {
            _productService = productService;
            _mapper = mapper;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            if (id == Guid.Empty) return NotFound();
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            return View(new ProductViewModel());
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var productDto = _mapper.Map<ProductDto>(model);
            await _productService.CreateProductAsync(productDto, model.RedirectUri, model.PostLogoutRedirectUri);

            TempData["StatusMessage"] = "Produto e Aplicação de Segurança criados com sucesso.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(Guid id)
        {
            if (id == Guid.Empty) return NotFound();

            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            var model = _mapper.Map<ProductViewModel>(product);

            if (product.OpenIddictApplicationId.HasValue)
            {
                var app = await _productService.GetLinkedOidcApplicationAsync(id);
                if (app != null)
                {
                    model.LinkedApplication = app;
                    model.RedirectUri = app.RedirectUri;
                    model.PostLogoutRedirectUri = app.PostLogoutRedirectUri;
                }
            }
            else
            {
                var unassignedApps = await _productService.GetUnassignedOidcApplicationsAsync();
                model.AvailableApplications = new SelectList(unassignedApps, "Id", "DisplayName");
            }

            return View(model);
        }


        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ProductViewModel model)
        {
            if (id != model.Id) return NotFound();

            //ModelState.Remove("AvailableApplications");

            if (!ModelState.IsValid)
            {
                // repopula lista se der erro
                if (!model.OpenIddictApplicationId.HasValue)
                {
                    var unassignedApps = await _productService.GetUnassignedOidcApplicationsAsync();
                    model.AvailableApplications = new SelectList(unassignedApps, "Id", "DisplayName", model.OpenIddictApplicationId);
                }

                return View(model);
            }

            var productDto = _mapper.Map<ProductDto>(model);

            var oidcApplication = await _productService.GetOidcApplicationAsync(model.OpenIddictApplicationId?.ToString());

            var appDto = new OidcApplicationDto
            {
                Id = model.OpenIddictApplicationId?.ToString(),
                RedirectUri = model.RedirectUri ?? oidcApplication.RedirectUri,
                PostLogoutRedirectUri = model.PostLogoutRedirectUri ?? oidcApplication.PostLogoutRedirectUri,
            };

            await _productService.UpdateProductWithApplicationAsync(productDto, appDto);

            TempData["SuccessMessage"] = "Produto e aplicação atualizados com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // NOVA ACTION PARA DESVINCULAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlink(Guid id)
        {
            await _productService.UnlinkOidcApplicationAsync(id);
            TempData["SuccessMessage"] = "Aplicação de segurança desvinculada com sucesso.";
            return RedirectToAction("Edit", new { id });
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(Guid id)
        {
            if (id == Guid.Empty) return NotFound();
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _productService.DeleteProductAsync(id);
            TempData["StatusMessage"] = "Produto excluído com sucesso.";
            return RedirectToAction(nameof(Index));
        }
    }
}