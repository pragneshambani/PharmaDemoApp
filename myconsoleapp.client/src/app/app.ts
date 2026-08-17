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
  errorMessage: string = '';
  successMessage: string = '';

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

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadMedicines();
    this.loadSales();
  }

  loadMedicines() {
    const url = this.searchTerm
      ? `/api/medicines?search=${encodeURIComponent(this.searchTerm)}`
      : '/api/medicines';

    this.http.get<Medicine[]>(url).subscribe({
      next: (data) => (this.medicines = data),
      error: (err) => console.error('Error loading medicines:', err)
    });
  }

  loadSales() {
    this.http.get<SaleRecord[]>('/api/sales').subscribe({
      next: (data) => (this.sales = data),
      error: (err) => console.error('Error loading sales:', err)
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
