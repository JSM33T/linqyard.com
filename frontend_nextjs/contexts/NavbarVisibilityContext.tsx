"use client";

import React, { createContext, useContext, useState } from "react";

type NavContextType = {
  visible: boolean;
  setVisible: (v: boolean) => void;
};

const NavbarVisibilityContext = createContext<NavContextType>({
  visible: true,
  setVisible: () => {},
});

export function NavbarVisibilityProvider({
  children,
  defaultVisible = true,
}: {
  children: React.ReactNode;
  defaultVisible?: boolean;
}) {
  const [visible, setVisible] = useState<boolean>(defaultVisible);

  return (
    <NavbarVisibilityContext.Provider value={{ visible, setVisible }}>
      {children}
    </NavbarVisibilityContext.Provider>
  );
}

export function useNavbarVisibility() {
  return useContext(NavbarVisibilityContext);
}
