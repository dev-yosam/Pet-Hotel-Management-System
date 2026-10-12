import { createTheme } from '@mui/material/styles'

const theme = createTheme({
  palette: {
    primary: { main: '#24665a' },
    background: { default: '#f5f7f8', paper: '#ffffff' },
    text: { primary: '#24332f', secondary: '#576760' },
  },
  typography: {
    fontFamily: 'system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
    h4: { fontWeight: 600, fontSize: '1.75rem' },
    button: { textTransform: 'none' },
  },
  shape: { borderRadius: 0 },
})

export default theme
