import { ComponentFixture } from '@angular/core/testing';

export const InjectionPayload = '<img src="x" onerror="window.injected = true"><script>window.injected = true</script>';

/** Clear the payload  */
export function resetInjection(): void {
  delete (window as unknown as { injected?: boolean }).injected;
}

/** Check the payload was shown as text */
export async function expectShownAsPlainText(fixture: ComponentFixture<unknown>): Promise<void> {
  const element = fixture.nativeElement as HTMLElement;

  await new Promise(resolve => setTimeout(resolve, 50));

  expect(element.textContent).toContain(InjectionPayload);
  expect(element.querySelector('img, script')).toBeNull();
  expect((window as unknown as { injected?: boolean }).injected).toBeUndefined();
}
