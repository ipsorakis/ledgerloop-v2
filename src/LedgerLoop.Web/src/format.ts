export function money(value: number, currency: string): string {
  return `${value.toFixed(2)} ${currency}`;
}

export function amount(value: number): string {
  return value.toFixed(2);
}
