import { useQuery } from '@tanstack/react-query';
import { api } from '@/api/client';

export type DeliveryRates = {
  karachi: number;
  otherCities: number;
};

export const DEFAULT_DELIVERY_RATES: DeliveryRates = {
  karachi: 300,
  otherCities: 400,
};

export function useDeliveryRates() {
  const query = useQuery({
    queryKey: ['delivery-rates'],
    queryFn: () => api.getDeliveryRates(),
    staleTime: 5 * 60 * 1000,
  });

  const rates = query.data ?? DEFAULT_DELIVERY_RATES;

  const getCharge = (city: string) => {
    if (!city.trim()) return 0;
    return city.trim().toLowerCase() === 'karachi' ? rates.karachi : rates.otherCities;
  };

  const label = `Karachi Rs.${rates.karachi.toLocaleString('en-PK')} | Other cities Rs.${rates.otherCities.toLocaleString('en-PK')}`;
  const shortLabel = `Karachi Rs.${rates.karachi.toLocaleString('en-PK')} · Other cities Rs.${rates.otherCities.toLocaleString('en-PK')}`;

  return { rates, getCharge, label, shortLabel, isLoading: query.isLoading };
}
