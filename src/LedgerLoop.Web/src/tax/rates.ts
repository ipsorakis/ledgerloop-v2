import type { LineCategory } from '../api/types';

export type RateKind = 'standard' | 'reduced' | 'superReduced';

/**
 * Rate figures used by the editor so the totals move while the operator types.
 * The ledger remains the reference for the figures that are stored.
 */
export const rateTable: Record<string, Record<RateKind, number>> = {
  FR: { standard: 20, reduced: 10, superReduced: 2.1 },
  DE: { standard: 19, reduced: 7, superReduced: 5 },
  ES: { standard: 21, reduced: 10, superReduced: 4 },
  GB: { standard: 20, reduced: 5, superReduced: 0 }
};

export const sellerCountry = 'FR';

const categoryKinds: Record<LineCategory, RateKind> = {
  StandardGoods: 'standard',
  ProfessionalServices: 'standard',
  DigitalServices: 'standard',
  Shipping: 'standard',
  PrintedBooks: 'reduced',
  MedicalSupplies: 'reduced',
  FoodStaples: 'superReduced'
};

export function rateKindFor(category: LineCategory): RateKind {
  return categoryKinds[category] ?? 'standard';
}

export function ratePercentFor(country: string, category: LineCategory): number {
  const rates = rateTable[country.toUpperCase()] ?? rateTable[sellerCountry];
  return rates[rateKindFor(category)];
}

export const categoryLabels: Record<LineCategory, string> = {
  StandardGoods: 'Goods',
  ProfessionalServices: 'Professional services',
  DigitalServices: 'Digital services',
  Shipping: 'Shipping',
  PrintedBooks: 'Printed books',
  MedicalSupplies: 'Medical supplies',
  FoodStaples: 'Food staples'
};
