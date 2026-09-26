import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RoomApiService } from './room-api.service';

describe('RoomApiService', () => {
  let service: RoomApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(RoomApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('gets rooms from room', () => {
    service.GetRooms().subscribe();

    expect(http.expectOne({ method: 'GET', url: 'room' })).toBeTruthy();
  });

  it('posts a new room to room', () => {
    service.CreateRoom({ name: 'Kitchen' }).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'room' }).request.body).toEqual({ name: 'Kitchen' });
  });

  it('puts a rename to room/{id}', () => {
    service.RenameRoom('room-1', { name: 'Kitchen' }).subscribe();

    expect(http.expectOne({ method: 'PUT', url: 'room/room-1' })).toBeTruthy();
  });

  it('deletes room/{id}', () => {
    service.DeleteRoom('room-1').subscribe();

    expect(http.expectOne({ method: 'DELETE', url: 'room/room-1' })).toBeTruthy();
  });

  it('posts a pairing to room/{id}/device', () => {
    service.PairDevice('room-1', { macAddress: 'AABBCCDDEE01', name: 'Lamp' }).subscribe();

    expect(http.expectOne({ method: 'POST', url: 'room/room-1/device' })).toBeTruthy();
  });

  it('deletes room/{id}/device/{mac} to unpair', () => {
    service.UnpairDevice('room-1', 'AABBCCDDEE01').subscribe();

    expect(http.expectOne({ method: 'DELETE', url: 'room/room-1/device/AABBCCDDEE01' })).toBeTruthy();
  });
});
