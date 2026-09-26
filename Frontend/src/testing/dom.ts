import { ComponentFixture } from '@angular/core/testing';

/** Return the rendered text */
export function textOf(fixture: ComponentFixture<unknown>): string {
  return (fixture.nativeElement as HTMLElement).textContent ?? '';
}

/** Find a button by its visible text */
export function buttonWithText(fixture: ComponentFixture<unknown>, text: string): HTMLButtonElement | undefined {
  const buttons = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('button'));
  return buttons.find(button => button.textContent?.trim() === text);
}

/** Click a button by its visible text */
export function clickButton(fixture: ComponentFixture<unknown>, text: string): void {
  const button = buttonWithText(fixture, text);

  if (button === undefined) {
    throw new Error(`No button with the text "${text}".`);
  }

  button.click();
  fixture.detectChanges();
}

/** Type into the input or select bound */
export function enterValue(fixture: ComponentFixture<unknown>, controlName: string, value: string): void {
  const element = (fixture.nativeElement as HTMLElement).querySelector<HTMLInputElement | HTMLSelectElement>(`[formControlName="${controlName}"]`);

  if (element === null) {
    throw new Error(`No control named "${controlName}".`);
  }

  element.value = value;
  element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
  fixture.detectChanges();
}

/** Submit the form */
export function submitForm(fixture: ComponentFixture<unknown>): void {
  (fixture.nativeElement as HTMLElement).querySelector('form')?.dispatchEvent(new Event('submit'));
  fixture.detectChanges();
}
