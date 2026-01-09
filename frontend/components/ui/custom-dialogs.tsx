import { useState, useEffect } from "react"
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import { CheckCircle, AlertTriangle, XCircle, Info, X } from "lucide-react"
import { cn } from "@/lib/utils"

interface CustomAlertProps {
  isOpen: boolean
  onClose: () => void
  title?: string
  description: string
  type?: "info" | "warning" | "error" | "success"
  autoClose?: boolean
  autoCloseDelay?: number
}

export function CustomAlert({ 
  isOpen, 
  onClose, 
  title,
  description, 
  type = "info",
  autoClose = true,
  autoCloseDelay = 5000
}: CustomAlertProps) {
  useEffect(() => {
    if (isOpen && autoClose) {
      const timer = setTimeout(() => {
        onClose()
      }, autoCloseDelay)

      return () => clearTimeout(timer)
    }
  }, [isOpen, autoClose, autoCloseDelay, onClose])

  const getIcon = () => {
    switch (type) {
      case "success":
        return <CheckCircle className="h-5 w-5 text-green-500" />
      case "warning":
        return <AlertTriangle className="h-5 w-5 text-yellow-500" />
      case "error":
        return <XCircle className="h-5 w-5 text-red-500" />
      default:
        return <Info className="h-5 w-5 text-blue-500" />
    }
  }

  const getTypeStyles = () => {
    switch (type) {
      case "success":
        return "border-l-4 border-green-500 bg-green-50 dark:bg-green-900/20"
      case "warning":
        return "border-l-4 border-yellow-500 bg-yellow-50 dark:bg-yellow-900/20"
      case "error":
        return "border-l-4 border-red-500 bg-red-50 dark:bg-red-900/20"
      default:
        return "border-l-4 border-blue-500 bg-blue-50 dark:bg-blue-900/20"
    }
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={onClose}>
      <AlertDialogContent className={cn(
        "max-w-md animate-in fade-in-0 zoom-in-95 slide-in-from-bottom-2",
        getTypeStyles()
      )}>
        <AlertDialogHeader className="relative">
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 mt-0.5">
              {getIcon()}
            </div>
            <div className="flex-1">
              <AlertDialogTitle className="text-left">
                {title || "Decision Replay Alert"}
              </AlertDialogTitle>
              <AlertDialogDescription className="text-left mt-2">
                {description}
              </AlertDialogDescription>
            </div>
          </div>
          {autoClose && (
            <button
              onClick={onClose}
              className="absolute top-0 right-0 p-1 rounded-sm opacity-70 hover:opacity-100 transition-opacity"
            >
              <X className="h-4 w-4" />
            </button>
          )}
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogAction 
            onClick={onClose}
            className={cn(
              "transition-all duration-200",
              type === "success" && "bg-green-600 hover:bg-green-700",
              type === "warning" && "bg-yellow-600 hover:bg-yellow-700",
              type === "error" && "bg-red-600 hover:bg-red-700",
              type === "info" && "bg-blue-600 hover:bg-blue-700"
            )}
          >
            OK
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

interface CustomConfirmProps {
  isOpen: boolean
  onConfirm: () => void
  onCancel: () => void
  title?: string
  description: string
  confirmText?: string
  cancelText?: string
  variant?: "default" | "destructive"
}

export function CustomConfirm({
  isOpen,
  onConfirm,
  onCancel,
  title,
  description,
  confirmText = "Confirm",
  cancelText = "Cancel",
  variant = "default"
}: CustomConfirmProps) {
  const getIcon = () => {
    return variant === "destructive" ? (
      <AlertTriangle className="h-5 w-5 text-red-500" />
    ) : (
      <Info className="h-5 w-5 text-blue-500" />
    )
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={onCancel}>
      <AlertDialogContent className={cn(
        "max-w-md animate-in fade-in-0 zoom-in-95 slide-in-from-bottom-2",
        variant === "destructive" 
          ? "border-l-4 border-red-500 bg-red-50 dark:bg-red-900/20" 
          : "border-l-4 border-blue-500 bg-blue-50 dark:bg-blue-900/20"
      )}>
        <AlertDialogHeader>
          <div className="flex items-start gap-3">
            <div className="flex-shrink-0 mt-0.5">
              {getIcon()}
            </div>
            <div className="flex-1">
              <AlertDialogTitle className="text-left">
                {title || "Decision Replay Confirmation"}
              </AlertDialogTitle>
              <AlertDialogDescription className="text-left mt-2">
                {description}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>
        <AlertDialogFooter className="gap-2">
          <AlertDialogCancel 
            onClick={onCancel}
            className="transition-all duration-200 hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            {cancelText}
          </AlertDialogCancel>
          <AlertDialogAction 
            onClick={onConfirm}
            className={cn(
              "transition-all duration-200",
              variant === "destructive" 
                ? "bg-red-600 hover:bg-red-700 text-white" 
                : "bg-blue-600 hover:bg-blue-700 text-white"
            )}
          >
            {confirmText}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// Custom hook for easier usage
export function useCustomAlert() {
  const [alertState, setAlertState] = useState<{
    isOpen: boolean
    title?: string
    description: string
    type?: "info" | "warning" | "error" | "success"
    autoClose?: boolean
    autoCloseDelay?: number
  }>({
    isOpen: false,
    description: "",
    autoClose: true,
    autoCloseDelay: 5000
  })

  const showAlert = (
    description: string, 
    title?: string, 
    type?: "info" | "warning" | "error" | "success",
    options?: { autoClose?: boolean; autoCloseDelay?: number }
  ) => {
    setAlertState({
      isOpen: true,
      title,
      description,
      type,
      autoClose: options?.autoClose ?? true,
      autoCloseDelay: options?.autoCloseDelay ?? 5000
    })
  }

  const closeAlert = () => {
    setAlertState(prev => ({ ...prev, isOpen: false }))
  }

  const AlertComponent = () => (
    <CustomAlert
      isOpen={alertState.isOpen}
      onClose={closeAlert}
      title={alertState.title}
      description={alertState.description}
      type={alertState.type}
      autoClose={alertState.autoClose}
      autoCloseDelay={alertState.autoCloseDelay}
    />
  )

  return { showAlert, closeAlert, AlertComponent }
}

export function useCustomConfirm() {
  const [confirmState, setConfirmState] = useState<{
    isOpen: boolean
    title?: string
    description: string
    onConfirm: () => void
    confirmText?: string
    cancelText?: string
    variant?: "default" | "destructive"
  }>({
    isOpen: false,
    description: "",
    onConfirm: () => {}
  })

  const showConfirm = (
    description: string,
    onConfirm: () => void,
    options?: {
      title?: string
      confirmText?: string
      cancelText?: string
      variant?: "default" | "destructive"
    }
  ) => {
    setConfirmState({
      isOpen: true,
      description,
      onConfirm,
      title: options?.title,
      confirmText: options?.confirmText,
      cancelText: options?.cancelText,
      variant: options?.variant
    })
  }

  const closeConfirm = () => {
    setConfirmState(prev => ({ ...prev, isOpen: false }))
  }

  const handleConfirm = () => {
    confirmState.onConfirm()
    closeConfirm()
  }

  const ConfirmComponent = () => (
    <CustomConfirm
      isOpen={confirmState.isOpen}
      onConfirm={handleConfirm}
      onCancel={closeConfirm}
      title={confirmState.title}
      description={confirmState.description}
      confirmText={confirmState.confirmText}
      cancelText={confirmState.cancelText}
      variant={confirmState.variant}
    />
  )

  return { showConfirm, ConfirmComponent }
}