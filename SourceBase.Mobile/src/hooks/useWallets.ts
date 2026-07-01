import { useQuery } from "@tanstack/react-query";
import { walletsApi } from "../api/wallets";
import { qk } from "../utils/queryKeys";

export const useWallets = () =>
  useQuery({ queryKey: qk.wallets, queryFn: walletsApi.list });

export const useWallet = (id: string) =>
  useQuery({
    queryKey: qk.wallet(id),
    queryFn: () => walletsApi.get(id),
    enabled: !!id,
  });
