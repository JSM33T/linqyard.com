"use client";

import React, { createContext, useContext, useState } from "react";

type FooterContextType = {
  visible: boolean;
  setVisible: (v: boolean) => void;
};

const FooterVisibilityContext = createContext<FooterContextType>({
  visible: true,
  setVisible: () => {},
});

export function FooterVisibilityProvider({
  children,
  defaultVisible = true,
}: {
  children: React.ReactNode;
  defaultVisible?: boolean;
}) {
  const [visible, setVisible] = useState<boolean>(defaultVisible);

  return (
    <FooterVisibilityContext.Provider value={{ visible, setVisible }}>
      {children}
    </FooterVisibilityContext.Provider>
  );
}

export function useFooterVisibility() {
  return useContext(FooterVisibilityContext);
}
