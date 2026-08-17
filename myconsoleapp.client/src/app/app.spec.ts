import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { App } from './app';
import { Medicine, SaleRecord } from './models/medicine';

describe('App Component', () => {
  let component: App;
  let fixture: ComponentFixture<App>;
  let httpMock: HttpTestingController;

  const mockMedicines: Medicine[] = [
    { id: '1', fullName: 'Amoxicillin 500mg', brand: 'Pfizer', quantity: 50, price: 12.50, expiryDate: '2026-08-01', notes: 'Note 1' },
    { id: '2', fullName: 'Paracetamol 500mg', brand: 'GSK', quantity: 5, price: 4.99, expiryDate: '2026-10-01', notes: 'Note 2' },
    { id: '3', fullName: 'Ibuprofen 400mg', brand: 'Bayer', quantity: 25, price: 8.75, expiryDate: new Date(Date.now() + 10 * 86400000).toISOString(), notes: 'Note 3' }
  ];

  const mockSales: SaleRecord[] = [
    { id: 's1', medicineId: '1', medicineName: 'Amoxicillin 500mg', quantitySold: 2, unitPrice: 12.50, totalAmount: 25.00, saleDate: '2026-02-15T10:00:00Z' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [App],
      imports: [HttpClientTestingModule, FormsModule]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(App);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create the app component', () => {
    expect(component).toBeTruthy();
  });

  it('should load medicines and sales on initialization', () => {
    component.ngOnInit();

    const medReq = httpMock.expectOne((req) => req.url.includes('/api/medicines'));
    expect(medReq.request.method).toBe('GET');
    medReq.flush(mockMedicines);

    const saleReq = httpMock.expectOne((req) => req.url.includes('/api/sales'));
    expect(saleReq.request.method).toBe('GET');
    saleReq.flush(mockSales);

    expect(component.medicines.length).toBe(3);
    expect(component.sales.length).toBe(1);
  });

  it('isExpiringSoon should return true if expiry date is within 30 days', () => {
    const soonDate = new Date(Date.now() + 15 * 86400000).toISOString();
    const farDate = new Date(Date.now() + 100 * 86400000).toISOString();

    expect(component.isExpiringSoon(soonDate)).toBeTrue();
    expect(component.isExpiringSoon(farDate)).toBeFalse();
  });

  it('isLowStock should return true if quantity is less than 10', () => {
    expect(component.isLowStock(5)).toBeTrue();
    expect(component.isLowStock(10)).toBeFalse();
    expect(component.isLowStock(50)).toBeFalse();
  });

  it('filteredMedicines should filter list based on column filters', () => {
    component.medicines = mockMedicines;
    component.columnFilters.fullName = 'Para';

    expect(component.filteredMedicines.length).toBe(1);
    expect(component.filteredMedicines[0].fullName).toBe('Paracetamol 500mg');
  });

  it('filteredSales should filter sales history list', () => {
    component.sales = mockSales;
    component.salesColumnFilters.medicineName = 'Amox';

    expect(component.filteredSales.length).toBe(1);

    component.salesColumnFilters.medicineName = 'NonExisting';
    expect(component.filteredSales.length).toBe(0);
  });

  it('toggleSort should toggle ascending/descending sorting', () => {
    component.sortColumn = 'fullName';
    component.sortAscending = true;

    component.toggleSort('fullName');
    expect(component.sortAscending).toBeFalse();

    const req = httpMock.expectOne((req) => req.url.includes('/api/medicines'));
    req.flush(mockMedicines);
  });

  it('saveMedicine should post new medicine and reload inventory', () => {
    component.newMedicine = {
      fullName: 'Cetamol 250mg',
      brand: 'Brand C',
      quantity: 30,
      price: 6.50,
      expiryDate: '2026-12-31',
      notes: 'Test note'
    };

    component.saveMedicine();

    const postReq = httpMock.expectOne('/api/medicines');
    expect(postReq.request.method).toBe('POST');
    postReq.flush({ id: '4', ...component.newMedicine });

    const reloadReq = httpMock.expectOne((req) => req.url.includes('/api/medicines'));
    reloadReq.flush(mockMedicines);

    expect(component.successMessage).toBe('Medicine added successfully!');
  });

  it('recordSale should post sale request and reload lists', () => {
    component.selectedMedicineId = '1';
    component.saleQuantity = 2;

    component.recordSale();

    const postReq = httpMock.expectOne('/api/sales');
    expect(postReq.request.method).toBe('POST');
    expect(postReq.request.body).toEqual({ medicineId: '1', quantitySold: 2 });
    postReq.flush(mockSales[0]);

    const reloadMedsReq = httpMock.expectOne((req) => req.url.includes('/api/medicines'));
    reloadMedsReq.flush(mockMedicines);

    const reloadSalesReq = httpMock.expectOne((req) => req.url.includes('/api/sales'));
    reloadSalesReq.flush(mockSales);

    expect(component.successMessage).toBe('Sale recorded successfully!');
  });
});
