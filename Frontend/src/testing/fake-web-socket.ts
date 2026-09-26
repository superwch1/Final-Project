export class FakeWebSocket {
  static instances: FakeWebSocket[] = [];

  readonly sent: string[] = [];
  isClosed = false;

  onopen: (() => void) | null = null;
  onmessage: ((event: { data: string }) => void) | null = null;
  onclose: (() => void) | null = null;

  constructor(readonly url: string) {
    FakeWebSocket.instances.push(this);
  }

  /** Return the most recently opened socket */
  static latest(): FakeWebSocket {
    return FakeWebSocket.instances[FakeWebSocket.instances.length - 1];
  }

  /** Record a message sent by the app */
  send(message: string): void {
    this.sent.push(message);
  }

  /** Record that the app closed the socket */
  close(): void {
    this.isClosed = true;
  }

  /** Pretend the server accepted the connection */
  serverOpens(): void {
    this.onopen?.();
  }

  /** Pretend the server sent a message */
  serverSends(data: object): void {
    this.onmessage?.({ data: JSON.stringify(data) });
  }

  /** Pretend the server dropped the connection */
  serverCloses(): void {
    this.onclose?.();
  }
}
