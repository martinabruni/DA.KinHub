import { useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { buildKinListDetailUrl, buildKinListRootUrl } from '@/config/appLinks'

export function ShoppingListsRedirect() {
  useEffect(() => {
    window.location.assign(buildKinListRootUrl())
  }, [])

  return null
}

export function ShoppingListDetailRedirect() {
  const { id } = useParams<{ id: string }>()

  useEffect(() => {
    window.location.assign(id ? buildKinListDetailUrl(id) : buildKinListRootUrl())
  }, [id])

  return null
}
