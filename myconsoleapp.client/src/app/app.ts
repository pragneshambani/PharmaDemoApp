import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Medicine, SaleRecord } from './models/medicine';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  standalone: false,
  styleUrl: './app.css'
})
export class App implements OnInit {
  activeTab: 'inventory' | 'add' | 'sales' = 'inventory';

  // Inventory
  medicines: Medicine[] = [];
  searchTerm: string = '';
  sortColumn: string = 'fullName';
  sortAscending: boolean = true;
  errorMessage: string = '';
  successMessage: string = '';

  columnFilters = {
    fullName: '',
    brand: '',
    expiryDate: '',
    quantity: '',
    price: '',
    status: ''
  };

  // Form for New Medicine
  newMedicine: Partial<Medicine> = {
    fullName: '',
    brand: '',
    quantity: 0,
    price: 0,
    expiryDate: '',
    notes: ''
  };

  // Sales
  sales: SaleRecord[] = [];
  selectedMedicineId: string = '';
  saleQuantity: number = 1;
  salesSortColumn: string = 'saleDate';
  salesSortAscending: boolean = false;

  salesColumnFilters = {
    saleDate: '',
    medicineName: '',
    quantitySold: '',
    unitPrice: '',
    totalAmount: ''
  };

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadMedicines();
    this.loadSales();
  }

  loadMedicines() {
    const params = new URLSearchParams();
    if (this.searchTerm) params.append('search', this.searchTerm);
    if (this.sortColumn) {
      params.append('sortBy', this.sortColumn);
      params.append('isAscending', this.sortAscending.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `/api/medicines?${queryString}` : '/api/medicines';

    this.http.get<Medicine[]>(url).subscribe({
      next: (data) => (this.medicines = data),
      error: (err) => console.error('Error loading medicines:', err)
    });
  }

  toggleSort(column: string) {
    if (this.sortColumn === column) {
      this.sortAscending = !this.sortAscending;
    } else {
      this.sortColumn = column;
      this.sortAscending = true;
    }
    this.loadMedicines();
  }

  getSortIcon(column: string): string {
    if (this.sortColumn !== column) return ' ↕';
    return this.sortAscending ? ' ▲' : ' ▼';
  }

  loadSales() {
    const params = new URLSearchParams();
    if (this.salesSortColumn) {
      params.append('sortBy', this.salesSortColumn);
      params.append('isAscending', this.salesSortAscending.toString());
    }

    const queryString = params.toString();
    const url = queryString ? `/api/sales?${queryString}` : '/api/sales';

    this.http.get<SaleRecord[]>(url).subscribe({
      next: (data) => (this.sales = data),
      error: (err) => console.error('Error loading sales:', err)
    });
  }

  toggleSalesSort(column: string) {
    if (this.salesSortColumn === column) {
      this.salesSortAscending = !this.salesSortAscending;
    } else {
      this.salesSortColumn = column;
      this.salesSortAscending = true;
    }
    this.loadSales();
  }

  getSalesSortIcon(column: string): string {
    if (this.salesSortColumn !== column) return ' ↕';
    return this.salesSortAscending ? ' ▲' : ' ▼';
  }

  get filteredMedicines(): Medicine[] {
    return this.medicines.filter(m => {
      const matchName = !this.columnFilters.fullName || m.fullName.toLowerCase().includes(this.columnFilters.fullName.toLowerCase());
      const matchBrand = !this.columnFilters.brand || m.brand.toLowerCase().includes(this.columnFilters.brand.toLowerCase());
      const matchExpiry = !this.columnFilters.expiryDate || m.expiryDate.includes(this.columnFilters.expiryDate);
      const matchQty = !this.columnFilters.quantity || m.quantity.toString().includes(this.columnFilters.quantity);
      const matchPrice = !this.columnFilters.price || m.price.toString().includes(this.columnFilters.price);

      let matchStatus = true;
      if (this.columnFilters.status === 'expiring') {
        matchStatus = this.isExpiringSoon(m.expiryDate);
      } else if (this.columnFilters.status === 'low') {
        matchStatus = !this.isExpiringSoon(m.expiryDate) && this.isLowStock(m.quantity);
      } else if (this.columnFilters.status === 'ok') {
        matchStatus = !this.isExpiringSoon(m.expiryDate) && !this.isLowStock(m.quantity);
      }

      return matchName && matchBrand && matchExpiry && matchQty && matchPrice && matchStatus;
    });
  }

  get filteredSales(): SaleRecord[] {
    return this.sales.filter(s => {
      const matchDate = !this.salesColumnFilters.saleDate || s.saleDate.includes(this.salesColumnFilters.saleDate);
      const matchName = !this.salesColumnFilters.medicineName || s.medicineName.toLowerCase().includes(this.salesColumnFilters.medicineName.toLowerCase());
      const matchQty = !this.salesColumnFilters.quantitySold || s.quantitySold.toString().includes(this.salesColumnFilters.quantitySold);
      const matchPrice = !this.salesColumnFilters.unitPrice || s.unitPrice.toString().includes(this.salesColumnFilters.unitPrice);
      const matchTotal = !this.salesColumnFilters.totalAmount || s.totalAmount.toString().includes(this.salesColumnFilters.totalAmount);

      return matchDate && matchName && matchQty && matchPrice && matchTotal;
    });
  }

  onSearchChange() {
    this.loadMedicines();
  }

  isExpiringSoon(expiryDateStr: string): boolean {
    if (!expiryDateStr) return false;
    const expiry = new Date(expiryDateStr);
    const today = new Date();
    const thirtyDaysFromNow = new Date();
    thirtyDaysFromNow.setDate(today.getDate() + 30);
    return expiry < thirtyDaysFromNow;
  }

  isLowStock(quantity: number): boolean {
    return quantity < 10;
  }

  saveMedicine() {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.newMedicine.fullName || !this.newMedicine.brand || !this.newMedicine.expiryDate) {
      this.errorMessage = 'Please fill in all required fields.';
      return;
    }

    this.http.post<Medicine>('/api/medicines', this.newMedicine).subscribe({
      next: () => {
        this.successMessage = 'Medicine added successfully!';
        this.newMedicine = { fullName: '', brand: '', quantity: 0, price: 0, expiryDate: '', notes: '' };
        this.loadMedicines();
        setTimeout(() => (this.activeTab = 'inventory'), 1000);
      },
      error: (err) => {
        this.errorMessage = err.error || 'Failed to add medicine.';
      }
    });
  }

  recordSale() {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.selectedMedicineId || this.saleQuantity <= 0) {
      this.errorMessage = 'Please select a medicine and valid quantity.';
      return;
    }

    const payload = {
      medicineId: this.selectedMedicineId,
      quantitySold: this.saleQuantity
    };

    this.http.post<SaleRecord>('/api/sales', payload).subscribe({
      next: () => {
        this.successMessage = 'Sale recorded successfully!';
        this.saleQuantity = 1;
        this.loadMedicines();
        this.loadSales();
      },
      error: (err) => {
        this.errorMessage = typeof err.error === 'string' ? err.error : 'Failed to record sale.';
      }
    });
  }
}
