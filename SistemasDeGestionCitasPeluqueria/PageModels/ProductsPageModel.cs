using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemasDeGestionCitasPeluqueria.Models;
using SistemasDeGestionCitasPeluqueria.Services;

namespace SistemasDeGestionCitasPeluqueria.PageModels
{
    public partial class ProductsPageModel(IInventoryService inventoryService, IProductCategoryService categoryService) : ObservableObject
    {
        private readonly IInventoryService _inventoryService = inventoryService;
        private readonly IProductCategoryService _categoryService = categoryService;

        private List<InventoryItem> _all = [];
        private ProductCategory _allCategory = new() { Id = 0, Name = "Todos", Order = int.MinValue };

        [ObservableProperty] private ObservableCollection<InventoryItem> products = [];
        [ObservableProperty] private ObservableCollection<ProductCategory> categories = [];
        [ObservableProperty] private ProductCategory? selectedCategory;
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? error;

        [RelayCommand]
        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (IsBusy) return;
            try
            {
                IsBusy = true;
                Error = null;

                // 1) Cargar categorías desde la API
                if (Categories.Count == 0)
                {
                    var cats = await _categoryService.GetAllAsync(ct);
                    var ordered = cats.OrderBy(c => c.Order).ToList();

                    // Insertar "Todos" 
                    ordered.Insert(0, _allCategory);

                    Categories = new ObservableCollection<ProductCategory>(ordered);

                    // Mantener la selección; por defecto "Todos"
                    SelectedCategory ??= _allCategory;
                }

                // 2) Cargar productos desde la API
                var items = await _inventoryService.GetAllAsync(ct);
                _all = items.ToList();

                ApplyFilter();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { Error = ex.Message; }
            finally { IsBusy = false; }
        }

        // Se invoca al tocar un chip
        [RelayCommand]
        private void SelectCategory(ProductCategory? category)
        {
            if (category is null) return;
            if (!ReferenceEquals(SelectedCategory, category))
                SelectedCategory = category;
        }

        partial void OnSelectedCategoryChanged(ProductCategory? value) => ApplyFilter();

        private void ApplyFilter()
        {
            var selected = SelectedCategory ?? _allCategory;

            IEnumerable<InventoryItem> query = _all;

            // Filtrado por CategoryId real. "Todos" usa Id=0.
            if (selected.Id != 0)
            {
                query = _all.Where(p => p.CategoryId == selected.Id);
            }

            Products = new ObservableCollection<InventoryItem>(query);
        }
    }
}
