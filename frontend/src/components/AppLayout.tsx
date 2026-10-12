import { useState } from 'react'
import {
  AppBar, Box, Button, Divider, Drawer, List, ListItemButton,
  ListItemText, Toolbar, Typography, useMediaQuery, useTheme,
} from '@mui/material'
import { NavLink, Outlet, useLocation } from 'react-router'

const drawerWidth = 256
const navigation = [
  { label: 'Dashboard', path: '/dashboard' },
  { label: 'Pet Check-in', path: '/pet-check-in' },
  { label: 'Daily Care', path: '/daily-care' },
  { label: 'Stay History', path: '/stay-history' },
]

function AppLayout() {
  const [menuOpen, setMenuOpen] = useState(false)
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('md'))
  const { pathname } = useLocation()

  const menu = (
    <>
      <Box sx={{ p: 3 }}>
        <Typography variant="h6" component="div" color="primary" sx={{ fontWeight: 700 }}>
          Pet Hotel
        </Typography>
        <Typography variant="body2" color="text.secondary">Management System</Typography>
      </Box>
      <Divider />
      <List component="nav" aria-label="Main navigation" sx={{ p: 2 }}>
        {navigation.map((item) => (
          <ListItemButton
            key={item.path}
            component={NavLink}
            to={item.path}
            selected={pathname === item.path}
            onClick={() => setMenuOpen(false)}
            sx={{ mb: 1 }}
          >
            <ListItemText primary={item.label} />
          </ListItemButton>
        ))}
      </List>
    </>
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100dvh' }}>
      <a className="skip-link" href="#main-content">Skip to content</a>
      <AppBar
        position="fixed"
        color="inherit"
        elevation={0}
        sx={{
          width: { md: 'calc(100% - 256px)' },
          ml: { md: drawerWidth + 'px' },
          borderBottom: 1,
          borderColor: 'divider',
        }}
      >
        <Toolbar sx={{ gap: 2 }}>
          <Button
            onClick={() => setMenuOpen(true)}
            aria-label="Open navigation menu"
            aria-expanded={menuOpen}
            aria-controls={menuOpen ? 'mobile-navigation' : undefined}
            sx={{ display: { md: 'none' } }}
          >
            Menu
          </Button>
          <Typography component="div" sx={{ fontWeight: 600 }}>Pet Hotel Management</Typography>
        </Toolbar>
      </AppBar>

      <Box component="aside" sx={{ width: { md: drawerWidth }, flexShrink: 0 }}>
        <Drawer
          variant="temporary"
          open={menuOpen && !isDesktop}
          onClose={() => setMenuOpen(false)}
          sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: drawerWidth } }}
        >
          <Box id="mobile-navigation">
            <Box sx={{ px: 2, pt: 2 }}>
              <Button onClick={() => setMenuOpen(false)}>Close menu</Button>
            </Box>
            {menu}
          </Box>
        </Drawer>
        <Drawer
          variant="permanent"
          sx={{ display: { xs: 'none', md: 'block' }, '& .MuiDrawer-paper': { width: drawerWidth } }}
        >
          {menu}
        </Drawer>
      </Box>

      <Box component="main" id="main-content" tabIndex={-1} sx={{ flexGrow: 1, minWidth: 0 }}>
        <Toolbar />
        <Box sx={{ p: { xs: 2, sm: 3, lg: 4 }, maxWidth: 1440, mx: 'auto' }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}

export default AppLayout
