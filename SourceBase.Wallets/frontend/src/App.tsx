import { withAuthenticationRequired } from "@auth0/auth0-react";
import { Route, Routes } from "react-router-dom";
import { Toasts } from "./components/Toasts";
import { TopBar } from "./components/TopBar";
import { CategoriesPage } from "./pages/categories/CategoriesPage";
import { WalletsPage } from "./pages/wallets/WalletsPage";

const FullScreenSpinner = () => <div className="loading-screen"><div className="loading-spinner"></div></div>;

const Shell = () => (
  <div className="flex h-screen bg-gray-50 dark:bg-gray-900">
    <div className="flex-1 flex flex-col min-w-0 overflow-hidden">
      <TopBar />
      <main className="flex-1 overflow-y-auto p-2 md:p-6">
        <Routes>
          <Route path="/" element={<WalletsPage />} />
          <Route path="/categories" element={<CategoriesPage />} />
        </Routes>
      </main>
    </div>
    <Toasts />
  </div>
);

/** Anonymous visitors are sent to the Auth0 hosted login; there is no login page of our own. */
export const App = withAuthenticationRequired(Shell, { onRedirecting: FullScreenSpinner });
